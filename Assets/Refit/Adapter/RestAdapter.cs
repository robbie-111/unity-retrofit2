using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Refit.Attributes.Http;
using Refit.Attributes.Params;
using Refit.Core;
using Refit.Http;
using Refit.Settings;
using Debug = UnityEngine.Debug;
using HeaderAttribute = Refit.Attributes.Params.HeaderAttribute;
using PropertyAttribute = Refit.Attributes.Params.PropertyAttribute;

namespace Refit.Adapter
{
    /// <summary>
    /// Refit for Unity 핵심 추상 클래스.
    ///
    /// <b>Refit 원본 대비 구현된 주요 기능:</b>
    /// <list type="bullet">
    ///   <item>[Headers("Key: Value")] — Refit 원본 스타일 정적 헤더, 제거/빈값 지원</item>
    ///   <item>[Header] 동적 헤더 / [HeaderCollection] 다중 헤더 / [Authorize] Bearer 단축</item>
    ///   <item>[AliasAs] — URL/쿼리/폼/멀티파트 이름 재정의</item>
    ///   <item>[Body(BodySerializationMethod.UrlEncoded)] — POCO 통째로 폼 직렬화</item>
    ///   <item>[Query(CollectionFormat.Csv)] — 배열 쿼리 포맷</item>
    ///   <item>[QueryUriFormat(Unescaped)] — URL 이스케이핑 제어</item>
    ///   <item>[Property] — RequestModifier 로 상태 전달</item>
    ///   <item>[AllowAnyStatusCode] — 비 2xx 억제</item>
    ///   <item>ApiResponse&lt;T&gt; / IApiResponse&lt;T&gt; — HasRequestError/HasResponseError</item>
    ///   <item>ApiException vs ApiRequestException 분리</item>
    ///   <item>RefitSettings — ContentSerializer, AuthorizationHeaderValueGetter, ExceptionFactory</item>
    ///   <item>StreamPart / ByteArrayPart / FilePart — Multipart 래퍼 타입</item>
    ///   <item>CancellationToken 파라미터 자동 감지</item>
    /// </list>
    /// </summary>
    public abstract class RestAdapter : MonoBehaviour
    {
        // ── 서브클래스 오버라이드 ────────────────────────────────────────

        protected abstract string SetBaseUrl();
        protected abstract Type SetApiInterface();
        protected virtual string SetBasePath() => string.Empty;
        protected virtual RefitSettings SetRefitSettings() => new RefitSettings();

        // ── 내부 상태 ────────────────────────────────────────────────────

        private string _baseUrl;
        private string _basePath;
        private RefitSettings _settings;
        private RestClient _client;
        private Type _apiInterface;
        private bool _interfaceAllowAnyStatusCode;

        // System.Reflection.MethodInfo 와 RequestInfo 를 모두 사용하므로
        // 완전한 타입 이름으로 명시하여 네임스페이스 충돌을 방지한다.
        private readonly Dictionary<string, RequestInfo> _cache = new Dictionary<string, RequestInfo>();

        // ── Unity 생명주기 ────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _baseUrl      = SetBaseUrl();
            _basePath     = SetBasePath();
            _settings     = SetRefitSettings() ?? new RefitSettings();
            _apiInterface = SetApiInterface();
            _client       = new RestClient(this, _settings);

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
                { Debug.LogError($"[Refit] 캐시 구성 실패 — {mi.Name}: {e.Message}"); }
            }
        }

        // ── 리플렉션 파싱 ────────────────────────────────────────────────

        private RequestInfo ParseMethod(System.Reflection.MethodInfo mi)
        {
            ValidateReturnType(mi);
            var info = new RequestInfo();

            // ── HTTP 메서드 & 경로 ──────────────────────────────────────
            bool found = false;
            foreach (Attribute attr in mi.GetCustomAttributes(true))
            {
                var httpMeta = attr.GetType()
                    .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .OfType<HttpMethodAttribute>().FirstOrDefault();

                if (httpMeta != null)
                {
                    info.Method     = httpMeta.Method;
                    info.MethodPath = attr.GetType().GetProperty("Path")?.GetValue(attr) as string ?? "";
                    found = true;
                }

                if (attr is MultipartAttribute ma)
                {
                    info.IsMultipart      = true;
                    info.MultipartBoundary = ma.Boundary;
                }

                if (attr is AllowAnyStatusCodeAttribute) info.AllowAnyStatusCode = true;
                if (attr is QueryUriFormatAttribute quf)
                    info.QueryUriUnescaped = quf.Format == UriFormat.Unescaped;
            }

            if (!found)
                throw new Exception($"[Refit] '{mi.Name}' 에 HTTP 메서드 어트리뷰트가 없습니다.");

            info.AllowAnyStatusCode |= _interfaceAllowAnyStatusCode;

            // ── 정적 헤더 (인터페이스 → 메서드 순으로 적용, 메서드가 덮어씀) ──
            ApplyHeadersAttrs(_apiInterface.GetCustomAttributes(typeof(HeadersAttribute), true), info);
            ApplyHeadersAttrs(mi.GetCustomAttributes(typeof(HeadersAttribute), true), info);

            // ── 파라미터 파싱 ───────────────────────────────────────────
            foreach (ParameterInfo pi in mi.GetParameters())
                ParseParam(pi, info);

            ValidateMethod(mi, info);
            return info;
        }

        private static void ApplyHeadersAttrs(object[] attrs, RequestInfo info)
        {
            foreach (HeadersAttribute ha in attrs.OfType<HeadersAttribute>())
                foreach (string raw in ha.Headers)
                    info.ApplyHeaderString(raw);
        }

        private static void ParseParam(ParameterInfo pi, RequestInfo info)
        {
            // CancellationToken 자동 감지
            if (pi.ParameterType == typeof(CancellationToken))
            {
                info.ParameterRoles.Add(RequestInfo.ParamRole.CancellationToken);
                info.ParameterKeys.Add(null);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            var attrs = pi.GetCustomAttributes(false).OfType<Attribute>().ToList();

            // AliasAs 이름 해소
            string aliasName = attrs.OfType<AliasAsAttribute>().FirstOrDefault()?.Name;

            Attribute role = attrs.FirstOrDefault(a =>
                a is QueryAttribute || a is PathAttribute    || a is BodyAttribute ||
                a is HeaderAttribute || a is HeaderCollectionAttribute ||
                a is AuthorizeAttribute || a is PartAttribute || a is PropertyAttribute);

            if (role == null)
            {
                // 어트리뷰트 없음 → Query (Refit 기본 동작)
                info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                info.ParameterKeys.Add(aliasName ?? pi.Name);
                info.ParameterNames.Add(pi.Name);
                return;
            }

            switch (role)
            {
                case QueryAttribute qa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Query);
                    info.ParameterKeys.Add(aliasName ?? qa.Name ?? pi.Name);
                    break;
                case PathAttribute pa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Path);
                    info.ParameterKeys.Add(aliasName ?? pa.Name ?? pi.Name);
                    break;
                case BodyAttribute ba:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Body);
                    info.ParameterKeys.Add(null);
                    info.HasBody = true;
                    info.BodySerializationMethod = ba.SerializationMethod; // 캐시에 저장
                    break;
                case HeaderAttribute ha:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Header);
                    info.ParameterKeys.Add(ha.Name);
                    break;
                case HeaderCollectionAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.HeaderCollection);
                    info.ParameterKeys.Add(null);
                    break;
                case AuthorizeAttribute aa:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Authorize);
                    info.ParameterKeys.Add(aa.Scheme);
                    break;
                case PartAttribute _:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Part);
                    info.ParameterKeys.Add(aliasName ?? pi.Name);
                    break;
                case PropertyAttribute pra:
                    info.ParameterRoles.Add(RequestInfo.ParamRole.Property);
                    info.ParameterKeys.Add(pra.Key ?? pi.Name);
                    break;
            }
            info.ParameterNames.Add(pi.Name);
        }

        private static void ValidateReturnType(System.Reflection.MethodInfo mi)
        {
            Type ret = mi.ReturnType;
            if (ret != typeof(Task) &&
                !(ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(Task<>)))
                throw new Exception($"[Refit] '{mi.Name}' 반환 타입은 Task 또는 Task<T> 이어야 합니다. 현재: {ret.Name}");
        }

        private static void ValidateMethod(System.Reflection.MethodInfo mi, RequestInfo info)
        {
            if (!info.IsMultipart) return;
            int parts = info.ParameterRoles.Count(r => r == RequestInfo.ParamRole.Part);
            if (parts == 0)
                throw new ArgumentException($"[Refit] [Multipart] '{mi.Name}' 에 [Part]/StreamPart/ByteArrayPart 파라미터가 없습니다.");
            if (info.HasBody)
                throw new ArgumentException($"[Refit] [Multipart] '{mi.Name}' 에 [Body] 는 사용할 수 없습니다.");
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
                throw new Exception($"[Refit] '{caller.Name}' 은 {_apiInterface.Name} 의 멤버가 아닙니다.");

            string key = ifaceMethod.ToString();
            if (!_cache.TryGetValue(key, out var info))
            {
                info = ParseMethod(ifaceMethod);
                _cache[key] = info;
            }
            return info;
        }

        // ── 공개 SendRequest 오버로드 ────────────────────────────────────

        protected Task<T> SendRequest<T>(params object[] arguments)
        {
            var info = GetCachedInfo();
            return Execute<T>(info, arguments);
        }

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

            RawResponse raw;
            try { raw = await _client.SendAsync(url, info, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                // ApiResponse<T> 반환이면 예외 대신 ApiResponse 에 담아 반환
                Type tType = typeof(T);
                if (IsApiResponseType(tType))
                    return BuildApiResponse<T>(null, new ApiRequestException(e.Message, url, e), tType);
                throw new ApiRequestException(e.Message, url, e);
            }

            // ApiResponse<T> / IApiResponse<T> 반환 타입 처리
            {
                Type tType = typeof(T);
                if (IsApiResponseType(tType))
                    return BuildApiResponse<T>(raw, null, tType);
            }

            // ApiException / 예외 처리
            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
            {
                await ThrowApiException(raw, info.AllowAnyStatusCode);
            }

            if (typeof(T) == typeof(string)) return (T)(object)raw.Body;

            return _settings.ContentSerializer.Deserialize<T>(raw.Body);
        }

        private async Task ExecuteVoid(RequestInfo info, object[] arguments)
        {
            info.ResetRuntimeData();
            var ct = FillRuntimeData(info, arguments);
            string url = info.BuildUrl(_baseUrl, _basePath);

            RawResponse raw;
            try { raw = await _client.SendAsync(url, info, ct); }
            catch (Exception e) { throw new ApiRequestException(e.Message, url, e); }

            if (!raw.IsSuccess && !info.AllowAnyStatusCode)
                await ThrowApiException(raw, info.AllowAnyStatusCode);
        }

        // ── ApiResponse<T> 빌더 ──────────────────────────────────────────

        private static bool IsApiResponseType(Type t)
        {
            if (t == typeof(IApiResponse)) return true;
            if (!t.IsGenericType) return false;
            var def = t.GetGenericTypeDefinition();
            return def == typeof(ApiResponse<>) || def == typeof(IApiResponse<>);
        }

        private T BuildApiResponse<T>(RawResponse raw, ApiExceptionBase error, Type tType)
        {
            // IApiResponse<TInner> or ApiResponse<TInner>
            Type innerType   = tType.IsGenericType ? tType.GetGenericArguments()[0] : typeof(object);
            Type concreteType = typeof(ApiResponse<>).MakeGenericType(innerType);

            // dynamic 대신 리플렉션 사용 — Microsoft.CSharp.dll 의존성 없음 (IL2CPP 안전)
            object resp = Activator.CreateInstance(concreteType);

            if (raw != null)
            {
                SetProp(resp, "StatusCode",  raw.StatusCode);
                SetProp(resp, "RawBody",     raw.Body);
                SetProp(resp, "IsReceived",  true);
                SetProp(resp, "Headers",
                    (IReadOnlyDictionary<string, string>)raw.ResponseHeaders
                    ?? new Dictionary<string, string>());

                if (raw.IsSuccess)
                {
                    SetProp(resp, "IsSuccessful", true);
                    try
                    {
                        if (!string.IsNullOrEmpty(raw.Body))
                        {
                            // IContentSerializer.Deserialize<TInner>(body) 를 리플렉션으로 호출
                            object data = _settings.ContentSerializer
                                .GetType()
                                .GetMethod("Deserialize")
                                ?.MakeGenericMethod(innerType)
                                .Invoke(_settings.ContentSerializer, new object[] { raw.Body });

                            SetProp(resp, "Content", data);
                        }
                    }
                    catch (Exception e)
                    {
                        SetProp(resp, "IsSuccessful", false);
                        SetProp(resp, "Error", new ApiException(
                            $"역직렬화 실패: {e.Message}", raw.Url, (int)raw.StatusCode, raw.Body));
                    }
                }
                else
                {
                    SetProp(resp, "IsSuccessful", false);
                    SetProp(resp, "Error", new ApiException(
                        raw.ErrorMessage ?? $"HTTP {raw.StatusCode}",
                        raw.Url, (int)raw.StatusCode, raw.Body));
                }
            }
            else if (error != null)
            {
                SetProp(resp, "IsReceived",   false);
                SetProp(resp, "IsSuccessful", false);
                SetProp(resp, "Error",        error);
            }

            return (T)resp;
        }

        /// <summary>리플렉션으로 internal setter 프로퍼티에 값을 주입한다.</summary>
        private static void SetProp(object obj, string propName, object value)
        {
            obj.GetType()
               .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)
               ?.SetValue(obj, value);
        }

        private async Task ThrowApiException(RawResponse raw, bool allowAny)
        {
            // ExceptionFactory 커스터마이징
            if (_settings.ExceptionFactory != null)
            {
                // ExceptionFactory 는 UnityWebRequest 가 아닌 RawResponse 기반으로 구성
                // Unity 환경에서는 팩토리에 null 을 넘기고 기본 예외 사용
                var customEx = await _settings.ExceptionFactory(null);
                if (customEx == null) return; // null = 억제
                throw customEx;
            }

            string msg = string.IsNullOrEmpty(raw.ErrorMessage)
                ? $"HTTP {raw.StatusCode}: {raw.Url}"
                : raw.ErrorMessage;

            if (raw.IsNetworkError)
                throw new ApiRequestException(msg, raw.Url);
            else
                throw new ApiException(msg, raw.Url, (int)raw.StatusCode, raw.Body);
        }

        // ── 런타임 파라미터 바인딩 ────────────────────────────────────────

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

                    case RequestInfo.ParamRole.Path:
                        if (key != null && arg != null)
                            info.PathParams[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.Body:
                        info.BodyInfo = new RequestInfo.BodySerializationInfo
                        {
                            Method   = info.BodySerializationMethod, // 캐시에서 읽음
                            RawValue = arg,
                        };
                        break;

                    case RequestInfo.ParamRole.Header:
                        if (key != null)
                            info.DynamicHeaders[key] = arg?.ToString();
                        break;

                    case RequestInfo.ParamRole.HeaderCollection:
                        if (arg is IDictionary<string, string> hcol)
                            foreach (var kv in hcol) info.DynamicHeaders[kv.Key] = kv.Value;
                        break;

                    case RequestInfo.ParamRole.Authorize:
                        // key = scheme (e.g. "Bearer")
                        if (arg != null)
                            info.DynamicHeaders["Authorization"] = $"{key} {arg}";
                        break;

                    case RequestInfo.ParamRole.Part:
                    {
                        string partName = key ?? info.ParameterNames[i];
                        string finalName = ResolveDynamicPartName(arg, partName);
                        info.MultipartItems.Add(new RequestInfo.MultipartItem
                        {
                            PartName = finalName,
                            Value    = arg,
                        });
                        break;
                    }

                    case RequestInfo.ParamRole.Property:
                        if (key != null && arg != null)
                            info.CustomProperties[key] = arg.ToString();
                        break;

                    case RequestInfo.ParamRole.CancellationToken:
                        if (arg is CancellationToken token) ct = token;
                        break;
                }
            }

            return ct;
        }

        private static string ResolveDynamicPartName(object value, string fallback)
        {
            // StreamPart.Name / ByteArrayPart.Name / FilePart.Name 이 우선
            if (value is StreamPart   sp && sp.Name   != null) return sp.Name;
            if (value is ByteArrayPart bp && bp.Name  != null) return bp.Name;
            if (value is FilePart      fp && fp.Name  != null) return fp.Name;
            return fallback;
        }
    }
}
