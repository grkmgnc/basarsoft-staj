using Npgsql;
using staj_proje.Model.Dto;
using staj_proje.Model.Entity;

namespace staj_proje.Model.Data
{
    public class GeometryRepository
    {
        private readonly string _connectionString;

        public GeometryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public void Add(IGeometry geometry)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO geometries (name, WKT) VALUES (@name, ST_GeomFromText(@wkt))", conn);
            cmd.Parameters.AddWithValue("@name", geometry.Name);
            cmd.Parameters.AddWithValue("@wkt", geometry.WKT);
            cmd.ExecuteNonQuery();
        }

        // Id ile tüm geometrileri getir
        public List<IGeometry> GetGeometriesAll()
        {
            var result = new List<IGeometry>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, ST_AsText(WKT) as WKT FROM geometries", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(GeometryFactory(reader));
            }
            return result;
        }

        // Type'a göre tüm geometrileri getir (type sütunu yok, WKT'den tip çıkarılır)
        public List<IGeometry> GetGeometriesByType(EGeometryType type)
        {
            var result = new List<IGeometry>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, ST_AsText(WKT) as WKT FROM geometries", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var geometry = GeometryFactory(reader);
                if (geometry.Type == type)
                    result.Add(geometry);
            }
            return result;
        }

        // Id ile tek geometri getir
        public IGeometry GetOneById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, ST_AsText(WKT) as WKT FROM geometries WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var geometry = GeometryFactory(reader);
                return geometry;
            }
            return null;
        }

        // Silme
        public bool DeleteGeometry(int id, EGeometryType type)
        {
            // type kontrolü uygulama tarafında yapılır, veritabanında sadece id ile silinir
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM geometries WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        // Güncelleme
        public IGeometry Update(int id, EGeometryType type, GeometryRequest request)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE geometries SET name = @name, WKT = ST_GeomFromText(@wkt) WHERE id = @id RETURNING id, name, ST_AsText(WKT) as WKT", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", request.Name);
            cmd.Parameters.AddWithValue("@wkt", request.WKT);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var geometry = GeometryFactory(reader);
                return geometry.Type == type ? geometry : null;
            }
            return null;
        }

        // Yardımcı: Okunan satırdan doğru IGeometry nesnesini oluşturur
        private IGeometry GeometryFactory(NpgsqlDataReader reader)
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var wkt = reader.GetString(2);

            // WKT'nin başına bakarak tipi belirle
            var typeString = wkt.Split('(')[0].Trim().ToUpper();
            EGeometryType type = typeString switch
            {
                "POINT" => EGeometryType.Point,
                "LINESTRING" => EGeometryType.LineString,
                "POLYGON" => EGeometryType.Polygon,
                _ => throw new Exception("Bilinmeyen geometri tipi")
            };

            return type switch
            {
                EGeometryType.Point => new Point { Id = id, Name = name, WKT = wkt, Type = type },
                EGeometryType.LineString => new LineString { Id = id, Name = name, WKT = wkt, Type = type },
                EGeometryType.Polygon => new Polygon { Id = id, Name = name, WKT = wkt, Type = type },
                _ => null
            };
        }


        private static int _nextId = 1;
        private static Dictionary<string, int> _Ids = new();
        public static List<Point> Points { get; } = new();
        public static List<LineString> LineStrings { get; } = new();
        public static List<Polygon> Polygons { get; } = new();
        public static int GetOrCreateCityId(string cityName)
        {
            if (_Ids.TryGetValue(cityName.ToLower(), out var id))
                return id;

            int newId = _nextId++;
            _Ids[cityName.ToLower()] = newId;
            return newId;
        }
    }
}
