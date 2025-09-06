using staj_proje.Model.Entity;
using System.Text.Json.Serialization;
namespace staj_proje.Model.Dto
{
    public class GeometryRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EGeometryType Type { get; set; }
        public string Name { get; set; } = "";   
        public string WKT { get; set; } = "";
    }
}
