using staj_proje.Model.Entity;
namespace staj_proje.Model
{
    public interface IGeometry
    {
        int Id { get; set; }
        string Name { get; set; }
        string WKT{ get; set; }
        EGeometryType Type { get; set; }
        bool IsValid();
    }
}
