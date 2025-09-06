using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using staj_proje.Model;
using staj_proje.Model.Data;
using staj_proje.Model.Dto;
using staj_proje.Model.Entity;
using staj_proje.Service;

namespace staj_proje.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class GeometryController : ControllerBase
    {
        private readonly IGeometryService _geometryService;

        public GeometryController(IGeometryService geometryService)
        {
            _geometryService = geometryService;
        }
        //Staj raporu için
        //public IActionResult AddGeometry([FromBody] IGeometry request)
        //{
        //    if (request == null || !request.IsValid())
        //        return BadRequest();
        //    IGeometry geometry = request.Type switch
        //    {
        //        "point" => new Point { Isim = request.Isim, WKT = request.WKT },
        //        "linestring"=> new LineString { Isim = request.Isim, WKT = request.WKT },
        //        "polygon"=> new Polygon { Isim = request.Isim, WKT = request.WKT },
        //        _ => null
        //    };

        //    if (geometry != null)
        //        geometry.Id = GeometryRepository.GetOrCreateCityId(geometry.Isim);

        //    return geometry;
        //}
        [HttpPost]
        public ResponseApi<GeometryResponse> AddGeometry([FromBody] GeometryRequest request)
        {
            try
            {
                var geometry = _geometryService.CreateGeometry(request);
                if (geometry == null || !geometry.IsValid())
                    return ResponseApi<GeometryResponse>.ErrorResponse(Messages.GeometryInvalid);

                _geometryService.AddGeometryRepo(geometry);

                var response = new GeometryResponse
                {
                    Type = geometry.Type,
                    Name = geometry.Name,
                    WKT = geometry.WKT,
                    Id = geometry.Id
                };
                return ResponseApi<GeometryResponse>.SuccessResponse(response, Messages.GeometryAdded);
            }
            catch
            {
                return ResponseApi<GeometryResponse>.ErrorResponse(Messages.UnexpectedError);
            }
        }

        [HttpGet("{type}")]
        public ResponseApi<List<IGeometry>> GetAll(EGeometryType type)
        {
            try
            {
                var geometries = _geometryService.GetGeometriesByType(type);
                if (!geometries.Any())
                    return ResponseApi<List<IGeometry>>.ErrorResponse(Messages.UnsupportedGeometryType);

                return ResponseApi<List<IGeometry>>.SuccessResponse(geometries, Messages.GeometryListSuccess);
            }
            catch
            {
                return ResponseApi<List<IGeometry>>.ErrorResponse(Messages.UnexpectedError);
            }
        }

        [HttpGet]
        public ResponseApi<List<GeometryResponse>> GetGeometriesAll()
        {
            try
            {
                var geometries = _geometryService.GetGeometriesAll();

                if (!geometries.Any())
                    return ResponseApi<List<GeometryResponse>>.ErrorResponse(Messages.GeometryNotFound);

                var responseList = geometries.Select(g => new GeometryResponse
                {
                    Id = g.Id,
                    Name = g.Name,
                    WKT = g.WKT,
                    Type = g.Type
                }).ToList();

                return ResponseApi<List<GeometryResponse>>.SuccessResponse(responseList, Messages.GeometryListSuccess);
            }
            catch
            {
                return ResponseApi<List<GeometryResponse>>.ErrorResponse(Messages.UnexpectedError);
            }
        }

        [HttpPost]
        public ResponseApi<List<GeometryResponse>> AddRange([FromBody] List<GeometryRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                    return ResponseApi<List<GeometryResponse>>.ErrorResponse(Messages.EmptyGeometryList);

                var addedGeometries = new List<GeometryResponse>();

                foreach (var request in requests)
                {
                    var geometry = _geometryService.CreateGeometry(request);
                    if (geometry == null || !geometry.IsValid())
                        continue;

                    _geometryService.AddGeometryRepo(geometry);

                    addedGeometries.Add(new GeometryResponse
                    {
                        Type = geometry.Type,
                        Name = geometry.Name,
                        WKT = geometry.WKT,
                        Id = geometry.Id
                    });
                }
                return ResponseApi<List<GeometryResponse>>.SuccessResponse(addedGeometries, Messages.GeometryListSuccess);
            }
            catch
            {
                return ResponseApi<List<GeometryResponse>>.ErrorResponse(Messages.UnexpectedError);
            }
        }

        [HttpPut("{id}/{type}")]
        public ResponseApi<GeometryResponse> Update(int id, EGeometryType type, [FromBody] GeometryRequest request)
        {
            try
            {
                if (request == null)
                    return ResponseApi<GeometryResponse>.ErrorResponse(Messages.GeometryInvalid);

                var updated = _geometryService.Update(id, type, request);

                if (updated == null)
                    return ResponseApi<GeometryResponse>.ErrorResponse(Messages.GeometryNotFound);

                return ResponseApi<GeometryResponse>.SuccessResponse(new GeometryResponse
                {
                    Id = updated.Id,
                    Name = updated.Name,
                    WKT = updated.WKT,
                    Type = updated.Type
                }, Messages.GeometryUpdated);
            }
            catch
            {
                return ResponseApi<GeometryResponse>.ErrorResponse(Messages.UnexpectedError);
            }
        }

        [HttpDelete("{id}/{type}")]
        public ResponseApi<string> Delete(int id, EGeometryType type)
        {
            try
            {
                bool removed = _geometryService.DeleteGeometry(id, type);

                if (!removed)
                    return ResponseApi<string>.ErrorResponse(Messages.GeometryNotFound);

                return ResponseApi<string>.SuccessResponse(null, Messages.GeometryDeleted);
            }
            catch
            {
                return ResponseApi<string>.ErrorResponse(Messages.UnexpectedError);
            }
        }
    }
}
