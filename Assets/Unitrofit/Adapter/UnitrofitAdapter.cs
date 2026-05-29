using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unitrofit.Attributes.Http;
using Unitrofit.Converter;
using Unitrofit.Core;
using Unitrofit.Http;
using Debug = UnityEngine.Debug;

namespace Unitrofit.Adapter
{
    /// <summary>
    /// Unitrofit 핵심 추상 클래스.
    ///
    /// Builder 방식으로 사용한다:
    /// <code>
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .AddInterceptor(new AuthInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    ///
    /// var api = new UnitrofitAdapter.Builder()
    ///     .BaseUrl("https://api.example.com")
    ///     .Client(client)
    ///     .Converter(new JsonConverter())
    ///     .Build&lt;MyService&gt;(gameObject);
    /// </code>
    /// </summary>
    public abstract class UnitrofitAdapter : MonoBehaviour
    {
        // ── 내부 상태 ────────────────────────────────────────────────────

        private string         _baseUrl;
        private IConverter     _converter;
        private UnitrofitClient _client;
        private Type           _apiInterface;
        private bool           _interfaceAllowAnyStatusCode;

        private readonly Dictionary<string, RequestInfo> _cache = new Dictionary<string, RequestInfo>();

        // ── 내부 초기화 (Builder에서 호출) ───────────────────────────────

        internal void Init(
            string          baseUrl,
            IConverter      converter,
            UnitrofitClient client,
            Type            apiInterface)
        {
            _baseUrl      = baseUrl;
            _converter    = converter ?? new JsonConverter();
            _client       = client;
            _apiInterface = apiInterface;

            _interfaceAllowAnyStatusCode = _apiInterface
                .GetCustomAttributes(typeof(AllowAnyStatusCodeAttribute), true).Any();

            BuildCache();
        }

        // ── 메서드 캐시 ──────────────────────────────────────────────────

        private void BuildCache()
        {
            foreach (System.Reflection.MethodInfo mi in _apiInterface.GetMethods())
            {
                string key = mi.ToString();
                if (_cache.ContainsKey(key)) continue;
                try   { _cache[key] = ParseMethod(mi); }
                catch (Exception e)
                { Debug.LogError($"[Unitrofit] 캐시 구성 실패 — {mi.Name}: {e.Message}"); }
            }
        }

        // ── 리플렉션 파싱 ────────────────────────────────────────────────

        private RequestInfo ParseMethod(System.Reflection.MethodInfo mi)
        {
            var info = new RequestInfo();

            // ── 반환 타입 판별 ──────────────────────────────────────────
            info.ReturnType = ResolveReturnKind(mi);

            // ── HTTP 메서드 & 경로 ──────────────────────────────────────
            bool found = false;
            foreach (Attribute attr in mi.GetCustomAttributes(true))
            {
                var httpMeta = attr.GetType()
                    .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .OfType<HttpMethodAttribute>()
                    .FirstOrDefault();

                if (httpMeta != null)
                {
                    info.Method     = httpMeta.Method;
                    info.MethodPath = attr.GetType().GetProperty("Path")?.GetValue(attr) as string ?? "";
                    found = true;
                }

                if (attr is MultipartAttribute)          info.IsMultipart       = true;
                if (attr is AllowAnyStatusCodeAttribute) info.AllowAnyStatusCode = true;
            }

            if (!found)
                throw new Exception($"[Unitrofit] '{mi.Name}' 에 HTTP 메서드 어트리뷰트가 없습니다.");

            info.AllowAnyStatusCode |= _interfaceAllowAnyStatusCode;

            // ── 정적 헤더 (인터페이스 → 메서드 순) ─────────────────────
            ApplyHeadersAttrs(_apiInterface.GetCustomAttributes(typeof(HeadersAttribute), true), info);
            ApplyHeadersAttrs(mi.GetCustomAttributes(typeof(HeadersAttribute), true), info);

            // ── 파라미터 파싱 ───────────────────────────────────────────
            foreach (ParameterInfo pi in mi.GetParameters())
                ParseParam(pi, info);

            ValidateMethod(mi, info);
            return info;
        }

        private static RequestInfo.ReturnKind ResolveReturnKind(System.Reflection.MethodInfo mi)
        {
            Type ret = mi.ReturnType;

            if (ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(UniTask<>))
                return RequestInfo.ReturnKind.UniTaskOfT;

            if (ret == typeof(UniTask))
                return RequestInfo.ReturnKind.UniTask;

            if (ret == typeof(void))
            {
                var first = mi.GetParameters().FirstOrDefault();
                if (first != null && first.ParameterType.IsGenericType &&
                    first.ParameterType.GetGenericTypeDefinition() == typeof(Callback<>))
                    return RequestInfo.ReturnKind.Callback;
            }

            throw new Exception(
                $"[Unitrofit] '{mi.Name}' 반환 타입은 UniTask<T>, UniTask, " +
                $"또는 void(첫 파라미터 Callback<T>) 이어야 합니다. 현재: {ret.Name}");
        }

        private static void ApplyHeadersAttrs(object[] attrs, RequestInfo info)
        {
            foreach (HeadersAttribute ha in attrs.OfType<HeadersAttribute>())
                foreach (string raw in ha.Headers)
                    info.ApplyHeaderString(raw);
        }

        private static void ParseParam(ParameterInfo pi, RequestInfo info)
        {
            if (pi.ParameterType == typeof(CancellationToken))
            {
                info.ParameterRoles.Add(RequestInfo.ParamRole.CancellationToken);
                info.ParameterKeys.Add(null);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            if (pi.ParameterType.IsGenericType &&
                pi.ParameterType.GetGenericTypeDefinition() == typeof(Callback<>))
                return;

            var attrs = pi.GetCustomAttributes(false).OfType<Attribute>().ToList();

            Attribute role = attrs.FirstOrDefault(a =>
                a is Attributes.Params.PathAttribute      ||
                a is Attributes.Params.QueryAttribute     ||
                a is Attributes.Params.QueryMapAttribute  ||
                a is Attributes.Params.BodyAttribute      ||
                a is Attributes.Params.FieldAttribute     ||
                a is Attributes.Params.HeaderAttribute    ||
                a is Attributes.Params.PartAttribute);

            if (role == null)
            {
                Debug.LogWarning($"[Unitrofit] 파라미터 '{pi.Name}' 에 어트리뷰트가 없어 Query로 처리합니다.");
                info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                info.ParameterKeys.Add(pi.Name);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            switch (role)
            {
                case Attributes.Params.PathAttribute pa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Path);
                    info.ParameterKeys.Add(pa.Name ?? pi.Name);
                    break;
                case Attributes.Params.QueryAttribute qa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                    info.ParameterKeys.Add(qa.Name ?? pi.Name);
                    break;
                case Attributes.Params.QueryMapAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.QueryMap);
                    info.ParameterKeys.Add(null);
                    break;
                case Attributes.Params.BodyAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Body);
                    info.ParameterKeys.Add(null);
                    info.HasBody = true;
                    break;
                case Attributes.Params.FieldAttribute fa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Field);
                    info.ParameterKeys.Add(fa.Name ?? pi.Name);
                    break;
                case Attributes.Params.HeaderAttribute ha:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Header);
                    info.ParameterKeys.Add(ha.Name);
                    break;
                case Attributes.Params.PartAttribute pta:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Part);
                    info.ParameterKeys.Add(pta.Name ?? pi.Name);
                    break;
            }
            info.ParameterNames.Add(pi.Name);
        }

        private static void ValidateMethod(System.Reflection.MethodInfo mi, RequestInfo info)
        {
            if (!info.IsMultipart) return;
            int parts = info.ParameterRoles.Count(r => r == RequestInfo.ParamRole.Part);
            if (parts == 0)
                throw new ArgumentException($"[Unitrofit] [Multipart] '{mi.Name}' 에 [Part] 파라미터가 없습니다.");
            if (info.HasBody)
                throw new ArgumentException($"[Unitrofit] [Multipart] '{mi.Name}' 에 [Body] 는 사용할 수 없습니다.");
        }

        // ── 캐시 조회 ────────────────────────────────────────────────────

        private RequestInfo GetCachedInfo()
        {
            var stack  = new StackTrace();
            MethodBase caller = stack.GetFrame(2).GetMethod();

            System.Reflection.MethodInfo ifaceMethod = _apiInterface.GetMethod(
                caller.Name,
                caller.GetParameters().Select(p => p.ParameterType).ToArray());

            if (ifaceMethod == null)
                throw new Exception($"[Unitrofit] '{caller.Name}' 은 {_apiInterface.Name} 의 멤버가 아닙니다.");

            string key = ifaceMethod.ToString();
            if (!_cache.TryGetValue(key, out var info))
            {
                info = ParseMethod(ifaceMethod);
                _cache[key] = info;
            }
            return info;
        }

        // ── 공개 SendRequest 오버로드 ────────────────────────────────────

        /// <summary>UniTask&lt;T&gt; 반환 메서드용.</summary>
        protected UniTask<T> SendRequest<T>(params object[] arguments)
        {
            var info = GetCachedInfo();
            return ExecuteAsync<T>(info, arguments);
        }

        /// <summary>UniTask (void) 반환 메서드용.</summary>
        protected UniTask SendRequest(params object[] arguments)
        {
            var info = GetCachedInfo();
            return ExecuteVoidAsync(info, arguments);
        }

        /// <summary>Callback&lt;T&gt; 패턴용. 첫 인자가 Callback&lt;T&gt;여야 한다.</summary>
        protected void SendRequest<T>(Callback<T> callback, params object[] arguments)
        {
            var info = GetCachedInfo();
            ExecuteWithCallback(info, callback, arguments).Forget();
        }

        // ── 실행 파이프라인 ───────────────────────────────────────────────

        private async UniTask<T> ExecuteAsync<T>(RequestInfo info, object[] arguments)
        {
            info.ResetRuntimeData();
            var ct = FillRuntimeData(info, arguments);

            string url = info.BuildUrl(_baseUrl);
            var raw    = await _client.SendAsync(url, info, ct);

            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
                ThrowException(raw);

            if (typeof(T) == typeof(string))
                return (T)(object)(raw.Body ?? string.Empty);

            if (string.IsNullOrEmpty(raw.Body))
                return default;

            return _converter.FromBody<T>(raw.Body);
        }

        private async UniTask ExecuteVoidAsync(RequestInfo info, object[] arguments)
        {
            info.ResetRuntimeData();
            var ct = FillRuntimeData(info, arguments);

            string url = info.BuildUrl(_baseUrl);
            var raw    = await _client.SendAsync(url, info, ct);

            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
                ThrowException(raw);
        }

        private async UniTaskVoid ExecuteWithCallback<T>(RequestInfo info, Callback<T> callback, object[] arguments)
        {
            try
            {
                var result = await ExecuteAsync<T>(info, arguments);
                callback.OnSuccess?.Invoke(result);
            }
            catch (UnitrofitException ex)
            {
                callback.OnError?.Invoke(ex);
            }
            catch (Exception ex)
            {
                callback.OnError?.Invoke(new UnitrofitException(ex.Message, info.BuildUrl(_baseUrl), inner: ex));
            }
        }

        private static void ThrowException(RawResponse raw)
        {
            string msg = string.IsNullOrEmpty(raw.ErrorMessage)
                ? $"HTTP {raw.StatusCode}: {raw.Url}"
                : raw.ErrorMessage;

            throw new UnitrofitException(
                message:        msg,
                requestUrl:     raw.Url,
                statusCode:     (int)raw.StatusCode,
                rawBody:        raw.Body,
                isNetworkError: raw.IsNetworkError);
        }

        // ── 런타임 파라미터 바인딩 ────────────────────────────────────────

        private CancellationToken FillRuntimeData(RequestInfo info, object[] arguments)
        {
            var ct    = CancellationToken.None;
            int count = Math.Min(info.ParameterRoles.Count, arguments?.Length ?? 0);

            for (int i = 0; i < count; i++)
            {
                var role = info.ParameterRoles[i];
                var key  = info.ParameterKeys[i];
                var arg  = arguments[i];

                switch (role)
                {
                    case RequestInfo.ParamRole.Path:
                        if (key != null && arg != null)
                            info.PathParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Query:
                        if (key != null && arg != null)
                            info.QueryParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.QueryMap:
                        if (arg is Dictionary<string, string> qmap)
                            foreach (var kv in qmap)
                                info.QueryMapParams[kv.Key] = kv.Value;
                        break;

                    case RequestInfo.ParamRole.Body:
                        info.BodyJson = arg != null ? _converter.ToBody(arg) : string.Empty;
                        break;

                    case RequestInfo.ParamRole.Field:
                        if (key != null && arg != null)
                            info.FieldParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Header:
                        if (key != null && arg != null)
                            info.DynamicHeaders[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Part:
                        if (arg is MultipartBody mb)
                            info.FilePart = mb;
                        break;

                    case RequestInfo.ParamRole.CancellationToken:
                        if (arg is CancellationToken token)
                            ct = token;
                        break;
                }
            }
            return ct;
        }

        // ════════════════════════════════════════════════════════════════
        // Builder — Retrofit.Builder 대응
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// UnitrofitAdapter.Builder — API 레벨 설정 (BaseUrl, Client, Converter).
        /// <code>
        /// var api = new UnitrofitAdapter.Builder()
        ///     .BaseUrl("https://api.example.com")
        ///     .Client(client)
        ///     .Converter(new JsonConverter())
        ///     .Build&lt;MyService&gt;(gameObject);
        /// </code>
        /// </summary>
        public class Builder
        {
            private string          _baseUrl;
            private UnitrofitClient _client;
            private IConverter      _converter;
            private string          _goName;

            /// <summary>API 서버 BaseUrl. 필수.</summary>
            public Builder BaseUrl(string baseUrl)
            {
                if (string.IsNullOrEmpty(baseUrl))
                    throw new ArgumentException("[Unitrofit] BaseUrl은 비어있을 수 없습니다.");
                _baseUrl = baseUrl;
                return this;
            }

            /// <summary>
            /// UnitrofitClient 주입. 인터셉터/타임아웃 설정을 담은 client를 전달한다.
            /// 지정하지 않으면 인터셉터 없는 기본 client를 사용한다.
            /// </summary>
            public Builder Client(UnitrofitClient client)
            {
                _client = client ?? throw new ArgumentNullException(nameof(client));
                return this;
            }

            /// <summary>JSON 직렬화 구현체. 기본값 JsonConverter.</summary>
            public Builder Converter(IConverter converter)
            {
                _converter = converter ?? throw new ArgumentNullException(nameof(converter));
                return this;
            }

            /// <summary>생성될 GameObject의 이름. 지정하지 않으면 타입명을 사용한다.</summary>
            public Builder Name(string name)
            {
                _goName = name;
                return this;
            }

            /// <summary>
            /// 서비스 인스턴스를 생성한다.
            /// T는 UnitrofitAdapter를 상속하고 API 인터페이스를 구현하는 구체 클래스여야 한다.
            /// </summary>
            public T Build<T>(GameObject parent = null) where T : UnitrofitAdapter
            {
                if (string.IsNullOrEmpty(_baseUrl))
                    throw new InvalidOperationException("[Unitrofit] BaseUrl() 을 먼저 호출하세요.");

                string goName = string.IsNullOrEmpty(_goName)
                    ? $"Unitrofit[{typeof(T).Name}]"
                    : _goName;

                var go = new GameObject(goName);
                if (parent != null)
                    go.transform.SetParent(parent.transform);

                var client = _client ?? new UnitrofitClient.Builder().Build();

                var adapter = go.AddComponent<T>();
                adapter.Init(
                    _baseUrl,
                    _converter ?? new JsonConverter(),
                    client,
                    typeof(T).GetInterfaces().FirstOrDefault(i => i != typeof(IDisposable)) ?? typeof(T));

                return adapter;
            }
        }
    }
}
