using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// 파라미터나 POCO 프로퍼티의 이름을 URL/쿼리스트링/폼필드/멀티파트 파트 이름으로
    /// 매핑할 때 사용하는 이름 재정의 어트리뷰트.
    ///
    /// <para>우선순위: [AliasAs] > RefitSettings.UrlParameterKeyFormatter > 파라미터 이름</para>
    /// <para>주의: 응답 역직렬화에는 적용되지 않는다. 역직렬화에는 [JsonProperty] 를 사용할 것.</para>
    ///
    /// <example>
    /// // URL 경로 파라미터 alias
    /// [Get("/group/{id}/users")]
    /// Task&lt;List&lt;User&gt;&gt; GroupListAsync([AliasAs("id")] int groupId);
    ///
    /// // 쿼리스트링 키 alias
    /// [Get("/users")]
    /// Task&lt;List&lt;User&gt;&gt; GetUsersAsync([AliasAs("sort")] string sortOrder);
    ///
    /// // 폼 POST 필드 이름 alias
    /// public class Measurement { [AliasAs("tid")] public string WebPropertyId { get; set; } }
    ///
    /// // Multipart 파트 이름 alias
    /// [Multipart][Post("/upload")]
    /// Task UploadAsync([AliasAs("myPhoto")] StreamPart photo);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AliasAsAttribute : Attribute
    {
        public string Name { get; }
        public AliasAsAttribute(string name) { Name = name; }
    }
}
