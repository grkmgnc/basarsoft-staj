using staj_proje.Model;
using staj_proje.Model.Dto;
using staj_proje.Model.Entity;
namespace staj_proje.Service
{
    public interface IGeometryService
    {
        IGeometry CreateGeometry(GeometryRequest request);
        void AddGeometryRepo(IGeometry geometry);
        List<IGeometry> GetGeometriesAll();
        List<IGeometry> GetGeometriesByType(EGeometryType type);
        IGeometry GetOneById(int id);
        bool DeleteGeometry(int id, EGeometryType type);
        IGeometry Update(int id, EGeometryType type, GeometryRequest request);

    }
}
