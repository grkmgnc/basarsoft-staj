using staj_proje.Model;
using staj_proje.Model.Data;
using staj_proje.Model.Dto;
using staj_proje.Model.Entity;

namespace staj_proje.Service
{
    public class ADONetGeometryService : IGeometryService
    {
        private readonly GeometryRepository _repository;

        public ADONetGeometryService(GeometryRepository repository)
        {
            _repository = repository;
        }

        public IGeometry CreateGeometry(GeometryRequest request)
        {
            if (request == null) return null;
            return request.Type switch
            {
                EGeometryType.Point => new Point { Name = request.Name, WKT = request.WKT, Type = request.Type },
                EGeometryType.LineString => new LineString { Name = request.Name, WKT = request.WKT, Type = request.Type },
                EGeometryType.Polygon => new Polygon { Name = request.Name, WKT = request.WKT, Type = request.Type },
                _ => null
            };
        }

        public void AddGeometryRepo(IGeometry geometry)
        {
            _repository.Add(geometry);
        }

        public List<IGeometry> GetGeometriesAll()
        {
            return _repository.GetGeometriesAll();
        }

        public List<IGeometry> GetGeometriesByType(EGeometryType type)
        {
            return _repository.GetGeometriesByType(type);
        }

        public IGeometry GetOneById(int id)
        {
            return _repository.GetOneById(id);
        }

        public bool DeleteGeometry(int id, EGeometryType type)
        {
            
            return _repository.DeleteGeometry(id,type);
        }
        public IGeometry Update(int id, EGeometryType type, GeometryRequest request)
        {
           return _repository.Update(id,type,request);             
        }

    }
}