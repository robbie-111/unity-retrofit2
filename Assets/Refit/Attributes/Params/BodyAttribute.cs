using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// HTTP 요청 바디를 지정한다. <see cref="BodySerializationMethod"/> 에 따라
    /// 직렬화 방식이 결정된다.
    /// </summary>
    public enum BodySerializationMethod
    {
        /// <summary>기본값. RefitSettings.ContentSerializer 로 직렬화 (보통 JSON).</summary>
        Default,

        /// <summary>
        /// application/x-www-form-urlencoded 폼 인코딩.
        /// 파라미터 타입은 Dictionary&lt;string,object&gt; 또는 POCO 이어야 한다.
        /// POCO 프로퍼티에 [AliasAs] 를 붙이면 필드 이름 재정의 가능.
        /// </summary>
        UrlEncoded,

        /// <summary>string 을 JSON StringContent 로 래핑하여 전송.</summary>
        Json,
    }

    /// <summary>
    /// 파라미터를 HTTP 요청 바디로 전송한다.
    ///
    /// <list type="bullet">
    ///   <item><b>Default</b>: JSON 직렬화 (application/json)</item>
    ///   <item><b>UrlEncoded</b>: 폼 인코딩 (application/x-www-form-urlencoded)</item>
    ///   <item><b>Json</b>: string → StringContent(JSON)</item>
    /// </list>
    ///
    /// <example>
    /// // JSON 바디
    /// [Post("/users")] Task CreateAsync([Body] User user);
    ///
    /// // 폼 인코딩 — Dictionary
    /// [Post("/collect")] Task CollectAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary&lt;string,object&gt; data);
    ///
    /// // 폼 인코딩 — POCO
    /// [Post("/collect")] Task CollectAsync([Body(BodySerializationMethod.UrlEncoded)] Measurement m);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class BodyAttribute : Attribute
    {
        public BodySerializationMethod SerializationMethod { get; }

        /// <summary>
        /// 버퍼링 활성화 여부. true 면 Content-Length 헤더가 자동 설정된다.
        /// </summary>
        public bool Buffered { get; }

        public BodyAttribute(
            BodySerializationMethod serializationMethod = BodySerializationMethod.Default,
            bool buffered = false)
        {
            SerializationMethod = serializationMethod;
            Buffered = buffered;
        }
    }
}
