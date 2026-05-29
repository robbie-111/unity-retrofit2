using System;
using Refit.Core;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// URL 쿼리스트링 파라미터.
    /// 이름을 생략하면 파라미터 이름(또는 [AliasAs])을 키로 사용한다.
    ///
    /// <para>배열/컬렉션은 <see cref="CollectionFormat"/> 으로 직렬화 방식을 지정할 수 있다.</para>
    ///
    /// <example>
    /// // 기본
    /// [Get("/users")] Task GetAsync([Query] int page);
    ///
    /// // 키 재정의
    /// [Get("/users")] Task GetAsync([Query("p")] int page);
    ///
    /// // 배열 — Multi 포맷 (기본): ?ages=10&amp;ages=20
    /// [Get("/users")] Task GetAsync([Query(CollectionFormat.Multi)] int[] ages);
    ///
    /// // 배열 — Csv 포맷: ?ages=10%2C20
    /// [Get("/users")] Task GetAsync([Query(CollectionFormat.Csv)] int[] ages);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QueryAttribute : Attribute
    {
        /// <summary>쿼리 키. null 이면 파라미터 이름 사용.</summary>
        public string Name { get; }

        /// <summary>컬렉션 직렬화 포맷. 기본값: <see cref="CollectionFormat.Multi"/>.</summary>
        public CollectionFormat CollectionFormat { get; }

        public QueryAttribute(string name = null, CollectionFormat collectionFormat = CollectionFormat.Multi)
        {
            Name = name;
            CollectionFormat = collectionFormat;
        }

        public QueryAttribute(CollectionFormat collectionFormat)
        {
            CollectionFormat = collectionFormat;
        }
    }
}
