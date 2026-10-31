namespace nlwc.ems.Base.Core.Paging;

public class PagerEntity<T>
{
    public int Page { get; set; }
    public int Max { get; set; }
    public int Count { get; set; }
    public IEnumerable<T> Data { get; set; } = [];
}
