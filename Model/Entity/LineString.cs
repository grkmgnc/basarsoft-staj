using staj_proje.Model;
namespace staj_proje.Model.Entity
{
    public class LineString : IGeometry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WKT { get; set; }
        public EGeometryType Type { get; set; } = EGeometryType.LineString; 

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) && Name.Length <= 25 &&
                   WKT.StartsWith("LINESTRING(", StringComparison.OrdinalIgnoreCase) &&
                   WKT.EndsWith(")");
        }
    }
}
