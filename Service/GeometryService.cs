using staj_proje.Model;
using staj_proje.Model.Data;
using staj_proje.Model.Dto;
using staj_proje.Model.Entity;
namespace staj_proje.Service
{
    public class GeometryService : IGeometryService
    {
        public IGeometry CreateGeometry(GeometryRequest request)
        {
            if (request == null) return null;

            IGeometry geometry = request.Type switch
            {
                EGeometryType.Point => new Point { Name = request.Name, WKT = request.WKT },
                EGeometryType.LineString => new LineString { Name = request.Name, WKT = request.WKT },
                EGeometryType.Polygon=> new Polygon { Name = request.Name, WKT = request.WKT },
                _ => null
            };

            if (geometry != null)
                geometry.Id = GeometryRepository.GetOrCreateCityId(geometry.Name);

            return geometry;
        }

        public void AddGeometryRepo(IGeometry geometry)
        {
            switch (geometry.Type)
            {
                case EGeometryType.Point:
                    GeometryRepository.Points.Add((Point)geometry);
                    break;
                case EGeometryType.LineString:
                    GeometryRepository.LineStrings.Add((LineString)geometry);
                    break;
                case EGeometryType.Polygon:
                    GeometryRepository.Polygons.Add((Polygon)geometry);
                    break;
            }
        }
        public List<IGeometry> GetGeometriesAll()
        {
            var geometries = new List<IGeometry>();
            geometries.AddRange(GeometryRepository.Points);
            geometries.AddRange(GeometryRepository.LineStrings);
            geometries.AddRange(GeometryRepository.Polygons);
            return geometries;
        }
        public List<IGeometry> GetGeometriesByType(EGeometryType type)
        {
            return type switch
            {
                EGeometryType.Point => GeometryRepository.Points.Cast<IGeometry>().ToList(),
                EGeometryType.LineString => GeometryRepository.LineStrings.Cast<IGeometry>().ToList(),
                EGeometryType.Polygon => GeometryRepository.Polygons.Cast<IGeometry>().ToList(),
                _ => new List<IGeometry>()
            };
        }
        public IGeometry GetOneById(int id)
        {
            // Önce Point koleksiyonunda ara
            var geom = GeometryRepository.Points.FirstOrDefault(x => x.Id == id);
            if (geom != null) return geom;

            // Sonra LineString koleksiyonunda ara
            var line = GeometryRepository.LineStrings.FirstOrDefault(x => x.Id == id);
            if (line != null) return line;

            // Son olarak Polygon koleksiyonunda ara
            var poly = GeometryRepository.Polygons.FirstOrDefault(x => x.Id == id);
            if (poly != null) return poly;

            // Hiçbiri bulunamazsa null döndür
            return null;
        }
        public bool DeleteGeometry(int id, EGeometryType type)
        {
           
            return type switch
            {
                EGeometryType.Point => GeometryRepository.Points.RemoveAll(x => x.Id == id) > 0,
                EGeometryType.LineString=> GeometryRepository.LineStrings.RemoveAll(x => x.Id == id) > 0,
                EGeometryType.Polygon => GeometryRepository.Polygons.RemoveAll(x => x.Id == id) > 0,
                _ => false
            };
        }
            public IGeometry Update(int id, EGeometryType type, GeometryRequest request)
        {
            var geometry = GetOneById(id);
            if (geometry == null) return null;
            geometry.Name = request.Name;
            geometry.WKT = request.WKT;
            return geometry;
        }
    }
}