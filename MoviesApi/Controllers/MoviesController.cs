using MoviesApi.Dtos;

namespace MoviesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {   
        private readonly ApplicationDbContext _context;

        private new List<string> _allowedExtenstions = new List<string> { ".jpg", ".png"};
        private long _maxAllowedPosterSize = 1048576;
        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var movie = await _context.Movies.Include(i=>i.Genre).OrderBy(m=>m.Title).ToListAsync();
            return Ok(movie);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var movie = await _context.Movies.Include(m=>m.Genre).FirstOrDefaultAsync(m=>m.Id == id);
            if(movie == null)
                return NotFound();
            return Ok(movie);
        }
        [HttpGet("GetByGenreId")]
        public async Task<IActionResult> GetByGenreIdAsync(byte genreId)
        {
            var movies = await _context.Movies.OrderByDescending(m => m.Rate)
                .Include(x => x.Genre)
                .Where(s => s.GenreId == genreId).ToListAsync();
            if(movies == null)
                return NotFound();
            return Ok(movies);
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm]CreateMovieDto dto)
        {
            if (!_allowedExtenstions.Contains(Path.GetExtension(dto.Poster.FileName.ToLower())))
                return BadRequest("only .png and .jpg are allowed!");
            if (dto.Poster.Length > _maxAllowedPosterSize)
                return BadRequest("Max allowed size for poster is 1MB");
            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if(!isValidGenre)
                return BadRequest("Invalid Genre ID");
            using var dataStream = new MemoryStream();
            await dto.Poster.CopyToAsync(dataStream);
            var movie = new Movies()
            {
                GenreId = dto.GenreId,
                Title = dto.Title,
                Poster = dataStream.ToArray(),
                Rate = dto.Rate,
                Storeline = dto.Storeline,
                Year = dto.Year,
            };
            await _context.Movies.AddAsync(movie);
            _context.SaveChanges();
            return Ok(movie);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] UpdateMovieDto dto)
        {
            var movie = await _context.Movies.FindAsync(id);
            if(movie == null)
                return BadRequest($"No Movie Was Found With ID: {id}");
            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if (!isValidGenre)
                return BadRequest("Invalid Genre ID");
            if(dto.Poster != null)
            {
                if (!_allowedExtenstions.Contains(Path.GetExtension(dto.Poster.FileName.ToLower())))
                    return BadRequest("only .png and .jpg are allowed!");
                if (dto.Poster.Length > _maxAllowedPosterSize)
                    return BadRequest("Max allowed size for poster is 1MB");
                using var dataStream = new MemoryStream();
                await dto.Poster.CopyToAsync(dataStream);
                movie.Poster = dataStream.ToArray();
            }
            
            movie.Title = dto.Title;
            movie.Rate = (double)dto.Rate;
            movie.Storeline = dto.Storeline;
            movie.Year = (int)dto.Year;
            movie.GenreId = dto.GenreId;
            _context.SaveChanges();
            return Ok(movie);

        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var movie = await _context.Movies.SingleOrDefaultAsync(m => m.Id == id);
            if(movie == null)
                return BadRequest($"No Movie Was Found With ID: {id}");
            _context.Movies.Remove(movie);
            _context.SaveChanges();
            return Ok(movie);
        }
    }
}
