using System;
using System.Collections.Generic;
using System.Text;
#nullable enable
namespace Net.Code.Csv
{
    // credits: https://github.com/mcintyre321/OneOf
    public record OneOf<T0, T1>
    {
        readonly T0? _value0;
        readonly T1? _value1;
        readonly int _index;

        OneOf(int index, T0? value0 = default, T1? value1 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            if (_value0 == null && _value1 == null) throw new ArgumentNullException("one of either T0 or T1 must be non-null");
        }

        public bool IsT0 => _index == 0;
        public bool IsT1 => _index == 1;

        public T0 AsT0 => _index == 0 ? _value0! : throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
        public T1 AsT1 => _index == 1 ? _value1! : throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");

        public static implicit operator OneOf<T0, T1>(T0 t) => new OneOf<T0, T1>(0, value0: t);
        public static implicit operator OneOf<T0, T1>(T1 t) => new OneOf<T0, T1>(1, value1: t);

        public void Switch(Action<T0> f0, Action<T1> f1)
        {
            switch (_index)
            {
                case 0: f0(_value0!); break;
                case 1: f1(_value1!); break;
                default: throw new InvalidOperationException();
            }
        }

        public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1)
        {
            return _index switch
            {
                0 => f0(_value0!),
                1 => f1(_value1!),
                _ => throw new InvalidOperationException()
            };
        }

        public static OneOf<T0, T1> FromT0(T0 input) => input;
        public static OneOf<T0, T1> FromT1(T1 input) => input;


        public OneOf<TResult, T1> MapT0<TResult>(Func<T0, TResult> mapFunc)
        {
            return _index switch
            {
                0 => mapFunc(AsT0),
                1 => AsT1,
                _ => throw new InvalidOperationException()
            };
        }

        public OneOf<T0, TResult> MapT1<TResult>(Func<T1, TResult> mapFunc)
        {
            return _index switch
            {
                0 => AsT0,
                1 => mapFunc(AsT1),
                _ => throw new InvalidOperationException()
            };
        }

        public bool TryPickT0(out T0? value, out T1? remainder)
        {
            value = IsT0 ? AsT0 : default;
            remainder = _index switch
            {
                0 => default,
                1 => AsT1,
                _ => throw new InvalidOperationException()
            };
            return this.IsT0;
        }

        public bool TryPickT1(out T1? value, out T0? remainder)
        {
            value = IsT1 ? AsT1 : default;
            remainder = _index switch
            {
                0 => AsT0,
                1 => default,
                _ => throw new InvalidOperationException()
            };
            return this.IsT1;
        }

        public override string? ToString() =>
            _index switch
            {
                0 => _value0!.ToString(),
                1 => _value1!.ToString(),
                _ => throw new InvalidOperationException()
            };
    }
}
