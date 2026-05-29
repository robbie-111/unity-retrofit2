using System;

namespace RestEase.Attributes.Http
{
    /// <summary>
    /// 정적 헤더를 선언한다. RestEase 원본처럼 이름과 값을 분리된 인수로 받는다.
    /// <para>인터페이스 레벨: 모든 메서드에 적용.</para>
    /// <para>메서드 레벨: 해당 메서드에만 적용. 동일 키는 인터페이스 헤더를 덮어씀.</para>
    /// <example>
    /// [Header("User-Agent", "MyApp")]
    /// [Header("Cache-Control", "no-cache")]
    /// public interface IMyApi { ... }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        /// <summary>헤더 이름.</summary>
        public string Name { get; }

        /// <summary>
        /// 헤더 값. null 이면 동일 이름의 상위 헤더를 제거한다.
        /// </summary>
        public string Value { get; }

        public HeaderAttribute(string name, string value = null)
        {
            Name = name;
            Value = value;
        }
    }
}
