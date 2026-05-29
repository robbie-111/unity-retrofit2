using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Refit.Converter;

namespace Refit.Settings
{
    /// <summary>
    /// Refit for Unity 의 전역 설정 객체.
    /// RestAdapter 의 <c>SetRefitSettings()</c> 에서 반환하여 적용한다.
    ///
    /// <example>
    /// protected override RefitSettings SetRefitSettings() => new RefitSettings
    /// {
    ///     ContentSerializer = new JsonContentSerializer(myJsonSettings),
    ///     AuthorizationHeaderValueGetter = (req, ct) => GetTokenAsync(),
    ///     CollectionFormat = CollectionFormat.Csv,
    /// };
    /// </example>
    /// </summary>
    public class RefitSettings
    {
        // ── 직렬화 ────────────────────────────────────────────────────

        /// <summary>
        /// HTTP 바디 직렬화/역직렬화기.
        /// 기본값: <see cref="JsonContentSerializer"/> (Newtonsoft.Json).
        /// </summary>
        public IContentSerializer ContentSerializer { get; set; } = new JsonContentSerializer();

        // ── 쿼리 파라미터 ─────────────────────────────────────────────

        /// <summary>
        /// 배열/컬렉션 쿼리 파라미터의 전역 기본 직렬화 포맷.
        /// 개별 파라미터에 [Query(CollectionFormat.Csv)] 로 오버라이드 가능.
        /// 기본값: <see cref="Core.CollectionFormat.Multi"/>.
        /// </summary>
        public Core.CollectionFormat CollectionFormat { get; set; } = Core.CollectionFormat.Multi;

        // ── 인증 ─────────────────────────────────────────────────────

        /// <summary>
        /// Bearer 토큰을 매 요청 직전에 자동으로 주입하는 델리게이트.
        /// 반환한 문자열이 "Authorization: Bearer {value}" 헤더로 설정된다.
        ///
        /// <para>[Headers("Authorization: Bearer")] 를 인터페이스에 선언했을 때 자동 활성화.</para>
        /// <para>null 이면 미적용.</para>
        ///
        /// <example>
        /// AuthorizationHeaderValueGetter = async (req, ct) => await GetAccessTokenAsync()
        /// </example>
        /// </summary>
        public Func<UnityWebRequest, CancellationToken, Task<string>> AuthorizationHeaderValueGetter { get; set; }

        // ── 요청 수정 ────────────────────────────────────────────────

        /// <summary>
        /// 모든 요청 전송 직전에 호출되는 수정자 델리게이트.
        /// 헤더 추가, 로깅, 서명 등 공통 처리에 사용한다.
        /// null 이면 미적용.
        /// </summary>
        public Func<UnityWebRequest, CancellationToken, Task> RequestModifier { get; set; }

        // ── 예외 팩토리 ───────────────────────────────────────────────

        /// <summary>
        /// 비 2xx 응답 수신 시 throw 할 예외를 커스터마이징한다.
        /// null 을 반환하면 예외를 억제한다.
        /// null(기본값)이면 기본 ApiException 을 사용한다.
        /// </summary>
        public Func<UnityWebRequest, Task<Exception>> ExceptionFactory { get; set; }

        // ── 타임아웃 ─────────────────────────────────────────────────

        /// <summary>요청 타임아웃 (초). 기본값: 30초.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        // ── 디버그 ───────────────────────────────────────────────────

        public bool EnableDebug { get; set; } = false;
    }
}
