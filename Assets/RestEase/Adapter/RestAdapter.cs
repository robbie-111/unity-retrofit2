using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RestEase.Attributes.Http;
using RestEase.Attributes.Params;
using RestEase.Converter;
using RestEase.Core;
using RestEase.Http;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using HeaderAttribute = RestEase.Attributes.Http.HeaderAttribute;

namespace RestEase.Adapter
{
    /// <summary>
    /// RestEase for Unity 핵심 추상 클래스.
    ///
    /// Utrofit 과 동일한 RestAdapter 상속 방식을 유지하되
    /// API 는 RestEase 원본에 최대한 가깝게 설계했다.
    ///
    /// <b>신규 기능 (Utrofit 대비):</b>
    /// <list type="bullet">
    ///   <item>[Header("Key","Value")] — 이름·값 분리 정적 헤더</item>
    ///   <item>[AllowAnyStatusCode] — 비 2xx 억제</item>
    ///   <item>Task&lt;Response&lt;T&gt;&gt; 반환 — 상태코드 + 역직렬화 데이터</item>
    ///   <item>CancellationToken 파라미터 자동 감지</item>
    ///   <item>[BasePath] — 공통 경로 접두사</item>
    ///   <item>RequestModifier 델리게이트</item>
    /// </list>
    ///
    /// <example>
    /// public class MyService : RestAdapter, IMyApi
    /// {
    ///     protected override string SetBaseUrl() => "https://api.example.com";
    ///     protected override Type SetApiInterface() => typeof(IMyApi);
    ///
    ///     public Task&lt;User&gt; GetUserAsync(int id, CancellationToken ct)
    ///         => SendRequest&lt;User&gt;(id, ct);
    /// }
    /// </example>
    /// </summary>
    public abstract class RestAdapter : MonoBehaviour
    {
        // ── 서브클래스 오버라이드 ────────────────────────────────────────

        protected abstract string SetBaseUrl();
        protected abstract Type SetApiInterface();

        /// <summary>JSON 역직렬화기. 기본값: JsonResponseDeserializer.</summary>
        protected virtual IResponseDeserializer SetDeserializer() => new JsonResponseDeserializer();

        /// <summary>
        /// RequestModifier 델리게이트. 모든 요청 직전에 호출된다.
        /// null 이면 미적용.
        /// </summary>
        protected virtual Func<UnityWebRequest, CancellationToken, Task> SetRequestModifier() => null;

        protected virtual bool EnableDebug => false;

        // ── 내부 상태 ────────────────────────────────────────────────────

        private string _baseUrl;
        private string _basePath;
        private IResponseDeserializer _deserializer;
        private RestClient _restClient;
        private Type _apiInterface;
        private bool _interfaceAllowAnyStatusCode;

        // System.Reflection.MethodInfo 와 RestEase.Core.RequestInfo 모두 사용하므로
        // 완전한 타입 이름으로 명시한다.
        private readonly Dictionary<string, RequestInfo> _cache = new Dictionary<string, RequestInfo>();

        // ── Unity 생명주기 ────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _baseUrl      = SetBaseUrl();
            _deserializer = SetDeserializer();
            _apiInterface = SetApiInterface();

            // [BasePath] 파싱
            var basePathAttr = _apiInterface
                .GetCustomAttributes(typeof(BasePathAttribute), true)
                .OfType<BasePathAttribute>()
                .FirstOrDefault();
            _basePath = basePathAttr?.Path ?? string.Empty;

            // 인터페이스 레벨 [AllowAnyStatusCode]
            _interfaceAllowAnyStatusCode = _apiInterface
                .GetCustomAttributes(typeof(AllowAnyStatusCodeAttribute), true)
                .Any();

            _restClient = new RestClient(this)
            {
                EnableDebug     = EnableDebug,
                RequestModifier = SetRequestModifier(),
            };

            BuildCache();
        }

        // ── 메서드 캐시 ──────────────────────────────────────────────────

        private void BuildCache()
        {
            foreach (System.Reflection.MethodInfo mi in _apiInterface.GetMethods())
            {
                string key = mi.ToString();
                if (_cache.ContainsKey(key)) continue;
                try   { _cache[key] = ParseMethodInfo(mi); }
                catch (Exception e)
                { Debug.LogError($"[RestEase] 캐시 구성 실패 — {mi.Name}: {e.Message}"); }
            }
        }

        // ── 리플렉션 파싱 ────────────────────────────────────────────────

        private RequestInfo ParseMethodInfo(System.Reflection.MethodInfo mi)
        {
            var info = new RequestInfo();

            // ── 반환 타입 검증 ──────────────────────────────────────────
            ValidateReturnType(mi);

            // ── HTTP 메서드 & 경로 ──────────────────────────────────────
            bool found = false;
            foreach (Attribute attr in mi.GetCustomAttributes(true))
            {
                var httpAttr = attr.GetType()
                    .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .OfType<HttpMethodAttribute>()
                    .FirstOrDefault();

                if (httpAttr != null)
                {
                    info.Method     = httpAttr.Method;
                    info.MethodPath = GetPathFromAttribute(attr);
                    found = true;
                }

                if (attr is MultipartAttribute)       info.IsMultipart = true;
                if (attr is AllowAnyStatusCodeAttribute) info.AllowAnyStatusCode = true;
            }

            if (!found)
                throw new Exception($"[RestEase] '{mi.Name}' 에 HTTP 메서드 어트리뷰트가 없습니다.");

            info.AllowAnyStatusCode |= _interfaceAllowAnyStatusCode;

            // ── 정적 헤더 (인터페이스 레벨 먼저, 메서드 레벨이 덮어씀) ──
            CollectStaticHeaders(_apiInterface.GetCustomAttributes(typeof(HeaderAttribute), true), info);
            CollectStaticHeaders(mi.GetCustomAttributes(typeof(HeaderAttribute), true), info);

            // ── 파라미터 파싱 ───────────────────────────────────────────
            foreach (ParameterInfo pi in mi.GetParameters())
                ParseParameter(pi, info);

            // ── 유효성 검사 ─────────────────────────────────────────────
            ValidateMethodInfo(mi, info);

            return info;
        }

        private static string GetPathFromAttribute(Attribute attr)
        {
            // 각 HTTP 어트리뷰트의 Path 프로퍼티를 리플렉션으로 읽는다.
            var pathProp = attr.GetType().GetProperty("Path");
            return pathProp?.GetValue(attr) as string ?? string.Empty;
        }

        private static void CollectStaticHeaders(object[] attrs, RequestInfo info)
        {
            foreach (HeaderAttribute ha in attrs.OfType<HeaderAttribute>())
            {
                // null value = 이전 동일 이름 헤더 제거
                info.StaticHeaders.RemoveAll(h => h.Name == ha.Name);
                if (ha.Value != null)
                    info.StaticHeaders.Add((ha.Name, ha.Value));
            }
        }

        private static void ParseParameter(ParameterInfo pi, RequestInfo info)
        {
            // CancellationToken: 어트리뷰트 없이 타입으로 자동 감지
            if (pi.ParameterType == typeof(CancellationToken))
            {
                info.ParameterRoles.Add(RequestInfo.ParamRole.CancellationToken);
                info.ParameterKeys.Add(null);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            var attrs = pi.GetCustomAttributes(false);
            Attribute found = attrs
                .OfType<Attribute>()
                .FirstOrDefault(a =>
                    a is QueryAttribute || a is QueryMapAttribute || a is PathAttribute ||
                    a is BodyAttribute  || a is FieldAttribute    || a is HeaderParamAttribute ||
                    a is PartAttribute);

            if (found == null)
            {
                // 어트리뷰트 없는 파라미터는 파라미터 이름을 키로 Query 로 처리 (RestEase 동작)
                info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                info.ParameterKeys.Add(pi.Name);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            switch (found)
            {
                case QueryAttribute qa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                    info.ParameterKeys.Add(qa.Name ?? pi.Name);
                    break;
                case QueryMapAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.QueryMap);
                    info.ParameterKeys.Add(null);
                    break;
                case PathAttribute pa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Path);
                    info.ParameterKeys.Add(pa.Name ?? pi.Name);
                    break;
                case BodyAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Body);
                    info.ParameterKeys.Add(null);
                    info.HasBody = true;
                    break;
                case FieldAttribute fa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Field);
                    info.ParameterKeys.Add(fa.Name ?? pi.Name);
                    break;
                case HeaderParamAttribute hpa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.HeaderParam);
                    info.ParameterKeys.Add(hpa.Name);
                    break;
                case PartAttribute pta:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Part);
                    info.ParameterKeys.Add(pta.Name ?? pi.Name);
                    break;
            }
            info.ParameterNames.Add(pi.Name);
        }

        private static void ValidateReturnType(System.Reflection.MethodInfo mi)
        {
            Type ret = mi.ReturnType;
            bool isTask    = ret == typeof(Task);
            bool isTaskT   = ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(Task<>);
            if (!isTask && !isTaskT)
                throw new Exception($"[RestEase] '{mi.Name}' 반환 타입은 Task 또는 Task<T> 이어야 합니다. 현재: {ret.Name}");
        }

        private static void ValidateMethodInfo(System.Reflection.MethodInfo mi, RequestInfo info)
        {
            if (!info.IsMultipart) return;
            int parts = info.ParameterRoles.Count(r => r == RequestInfo.ParamRole.Part);
            if (parts == 0)
                throw new ArgumentException($"[RestEase] [Multipart] '{mi.Name}' 에 [Part] 파라미터가 없습니다.");
            if (parts > 1)
                throw new ArgumentException($"[RestEase] [Multipart] '{mi.Name}' 에 [Part] 파라미터는 하나만 허용됩니다.");
            if (info.HasBody)
                throw new ArgumentException($"[RestEase] [Multipart] '{mi.Name}' 에 [Body] 는 사용할 수 없습니다.");
        }

        // ── 캐시 조회 ────────────────────────────────────────────────────

        /// <summary>스택 트레이스로 호출자를 감지하고 캐시된 RequestInfo 를 반환한다.</summary>
        private RequestInfo GetCachedInfo()
        {
            var stack  = new StackTrace();
            MethodBase caller = stack.GetFrame(2).GetMethod();

            System.Reflection.MethodInfo ifaceMethod = _apiInterface.GetMethod(
                caller.Name,
                caller.GetParameters().Select(p => p.ParameterType).ToArray());

            if (ifaceMethod == null)
                throw new Exception($"[RestEase] '{caller.Name}' 은 {_apiInterface.Name} 의 멤버가 아닙니다.");

            string key = ifaceMethod.ToString();
            if (!_cache.TryGetValue(key, out var info))
            {
                info = ParseMethodInfo(ifaceMethod);
                _cache[key] = info;
            }
            return info;
        }

        // ── 공개 SendRequest 오버로드 ────────────────────────────────────

        /// <summary>Task&lt;T&gt; 반환 메서드용.</summary>
        protected Task<T> SendRequest<T>(params object[] arguments)
        {
            var info = GetCachedInfo();
            return Execute<T>(info, arguments);
        }

        /// <summary>Task (void) 반환 메서드용.</summary>
        protected Task SendRequest(params object[] arguments)
        {
            var info = GetCachedInfo();
            return ExecuteVoid(info, arguments);
        }

        // ── 실행 파이프라인 ───────────────────────────────────────────────

        private async Task<T> Execute<T>(RequestInfo info, object[] arguments)
        {
            info.ResetRuntimeData();
            var ct = FillRuntimeData(info, arguments);
            string url = info.BuildUrl(_baseUrl, _basePath);

            RawResponse raw = await _restClient.SendAsync(url, info, ct);

            // Task<Response<T>> 반환 타입 처리
            Type tType = typeof(T);
            if (tType.IsGenericType && tType.GetGenericTypeDefinition() == typeof(Response<>))
            {
                Type innerType = tType.GetGenericArguments()[0];
                return (T)BuildResponseObject(raw, innerType);
            }

            // ApiException 발생 여부 결정
            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
            {
                string msg = string.IsNullOrEmpty(raw.ErrorMessage)
                    ? $"HTTP {raw.StatusCode}"
                    : raw.ErrorMessage;
                throw new ApiException(msg, raw.Url, (int)raw.StatusCode, raw.Body, raw.IsNetworkError);
            }

            if (typeof(T) == typeof(string))
                return (T)(object)raw.Body;

            return _deserializer.Deserialize<T>(raw.Body);
        }

        private async Task ExecuteVoid(RequestInfo info, object[] arguments)
        {
            info.ResetRuntimeData();
            var ct = FillRuntimeData(info, arguments);
            string url = info.BuildUrl(_baseUrl, _basePath);

            RawResponse raw = await _restClient.SendAsync(url, info, ct);

            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
            {
                string msg = string.IsNullOrEmpty(raw.ErrorMessage)
                    ? $"HTTP {raw.StatusCode}"
                    : raw.ErrorMessage;
                throw new ApiException(msg, raw.Url, (int)raw.StatusCode, raw.Body, raw.IsNetworkError);
            }
        }

        /// <summary>Response&lt;TInner&gt; 인스턴스를 리플렉션으로 생성한다.</summary>
        private object BuildResponseObject(RawResponse raw, Type innerType)
        {
            Type responseType = typeof(Response<>).MakeGenericType(innerType);
            var response = Activator.CreateInstance(responseType);

            SetProp(response, "StatusCode",    raw.StatusCode);
            SetProp(response, "RawBody",       raw.Body);
            SetProp(response, "Headers",       raw.ResponseHeaders);
            SetProp(response, "IsNetworkError",raw.IsNetworkError);

            if (raw.IsNetworkError || raw.IsHttpError)
                SetProp(response, "ErrorMessage", raw.ErrorMessage);

            if (raw.IsSuccess && !string.IsNullOrEmpty(raw.Body))
            {
                try
                {
                    var data = _deserializer.GetType()
                        .GetMethod("Deserialize")
                        ?.MakeGenericMethod(innerType)
                        .Invoke(_deserializer, new object[] { raw.Body });
                    SetProp(response, "Data", data);
                }
                catch { /* 역직렬화 실패 시 Data 는 default */ }
            }

            return response;
        }

        private static void SetProp(object obj, string propName, object value)
        {
            obj.GetType().GetProperty(propName)?.SetValue(obj, value);
        }

        // ── 런타임 파라미터 바인딩 ────────────────────────────────────────

        /// <summary>arguments 를 RequestInfo 런타임 필드에 채우고 CancellationToken 을 반환한다.</summary>
        private CancellationToken FillRuntimeData(RequestInfo info, object[] arguments)
        {
            var ct = CancellationToken.None;
            int count = Math.Min(info.ParameterRoles.Count, arguments?.Length ?? 0);

            for (int i = 0; i < count; i++)
            {
                var role = info.ParameterRoles[i];
                var key  = info.ParameterKeys[i];
                var arg  = arguments[i];

                switch (role)
                {
                    case RequestInfo.ParamRole.Query:
                        if (key != null && arg != null)
                            info.QueryParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.QueryMap:
                        if (arg is Dictionary<string, string> qmap)
                            foreach (var kv in qmap) info.QueryMapParams[kv.Key] = kv.Value;
                        break;

                    case RequestInfo.ParamRole.Path:
                        if (key != null && arg != null)
                            info.PathParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Body:
                        info.BodyJson = arg != null ? _deserializer.Serialize(arg) : string.Empty;
                        break;

                    case RequestInfo.ParamRole.Field:
                        if (key != null && arg != null)
                            info.FieldParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.HeaderParam:
                        if (key != null && arg != null)
                            info.DynamicHeaders[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Part:
                        if (arg is FilePart fp)        info.FilePart = fp;
                        else if (arg is byte[] bytes)  info.FilePart = new FilePart(key ?? "file", "upload", bytes);
                        break;

                    case RequestInfo.ParamRole.CancellationToken:
                        if (arg is CancellationToken token) ct = token;
                        break;
                }
            }

            return ct;
        }
    }
}
