namespace Refit.Core
{
    /// <summary>
    /// 배열/컬렉션 쿼리 파라미터의 직렬화 형식.
    /// </summary>
    public enum CollectionFormat
    {
        /// <summary>키를 반복: ?ages=10&amp;ages=20&amp;ages=30 (기본값)</summary>
        Multi,

        /// <summary>쉼표 구분: ?ages=10%2C20%2C30</summary>
        Csv,

        /// <summary>공백(+) 구분: ?ages=10+20+30</summary>
        Ssv,

        /// <summary>탭 구분: ?ages=10%0920%0930</summary>
        Tsv,

        /// <summary>파이프 구분: ?ages=10%7C20%7C30</summary>
        Pipes,
    }
}
