using System.Collections;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;

/// <summary>
/// A reusable, growable list of <see cref="DiveSegment" /> values backed by a pooled
/// array, used to return the final ascent without allocating per call. The buffer also
/// implements <see cref="ICollection{T}" /> so that consumers copying it, such as
/// <c>List&lt;T&gt;.AddRange</c>, take the allocation-free <see cref="CopyTo" /> path
/// instead of boxing an enumerator. The backing array only grows, so a warmed instance
/// allocates nothing in steady state.
/// </summary>
internal sealed class SegmentBuffer : IReadOnlyList<DiveSegment>, ICollection<DiveSegment>
{
    private DiveSegment[] _items = new DiveSegment[64];

    /// <summary>Copies the buffered segments into the given array.</summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">The destination is too small to hold the segments.</exception>
    public void CopyTo(DiveSegment[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        Array.Copy(_items, 0, array, arrayIndex, Count);
    }

    bool ICollection<DiveSegment>.IsReadOnly => true;

    void ICollection<DiveSegment>.Add(DiveSegment item)
    {
        throw new NotSupportedException();
    }

    void ICollection<DiveSegment>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<DiveSegment>.Remove(DiveSegment item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<DiveSegment>.Contains(DiveSegment item)
    {
        for (var i = 0; i < Count; i++)
        {
            if (EqualityComparer<DiveSegment>.Default.Equals(_items[i], item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gets the number of segments currently in the buffer.</summary>
    public int Count { get; private set; }

    /// <summary>Gets the segment at the given index.</summary>
    /// <param name="index">The zero-based index of the segment.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the buffer.</exception>
    public DiveSegment this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return _items[index];
        }
    }

    /// <summary>Returns an enumerator over the buffered segments.</summary>
    /// <returns>An enumerator over the buffered segments.</returns>
    /// <remarks>Enumerating allocates; copying consumers use <see cref="CopyTo" /> instead.</remarks>
    public IEnumerator<DiveSegment> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return _items[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Appends a segment, growing the backing array geometrically when full.</summary>
    /// <param name="segment">The segment to append.</param>
    public void Add(in DiveSegment segment)
    {
        if (Count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[Count] = segment;
        Count++;
    }

    /// <summary>Empties the buffer without releasing the backing array.</summary>
    public void Clear()
    {
        Count = 0;
    }
}