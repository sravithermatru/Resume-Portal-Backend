using Aspose.Words;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;
using Resume_Selector_Page.Data;
using Resume_Selector_Page.Models;
using Resume_Selector_Page.Services;
using System;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using UglyToad.PdfPig;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Resume_Selector_Page.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResumesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileProvider fileProvider1;
        private const string UploadsFolder = "Uploads";


        public ResumesController(AppDbContext context, IWebHostEnvironment environment, IFileProvider fileProvider1,
                                    BlobServiceClient blobServiceClient)
        {
            this._context = context;
            _blobServiceClient = blobServiceClient;
            this._environment = environment;
            this.fileProvider1 = fileProvider1;
        }


      //  [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllProfile()
        {
            var resumes = await _context.ResumesData.Select(r => new
            {
                Id = r.Id,
                FileName = r.FileName,
                PdfUrl = Url.Action(nameof(DownloadResume), "ResumesData", new { id = r.Id }, Request.Scheme)
            }).ToListAsync();
            return Ok(resumes);
        }


     
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadResume(int id)
        {
            var resume = _context.ResumesData.FirstOrDefault(x => x.Id == id);
            if (resume == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(resume.FileName))
            {
                return NotFound();
            }


            //Retrieve Azure Blob Storage container
            var containerName = "atsfilestorage";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);


           // track download
            //var recruiterid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (recruiterid == null)
            //{
            //    return Unauthorized();
            //}
            //var downloadedResume = new DownloadedResume
            //{
            //    RecruiterId = "0",
            //   ResumeId = 2,
            //    DownloadedAt = DateTime.UtcNow,
            //};
            //_context.DownloadedResumes.Add(downloadedResume);
            //await _context.SaveChangesAsync();


            //create a blob client
            var blobClient = containerClient.GetBlobClient(resume.FileName);

            if(!await blobClient.ExistsAsync())
            {
                return NotFound("File not found");
            }

            //Download the file from the blob Storage
            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;

            var mimeType = "application/pdf";
            return File(stream, mimeType, resume.FileName);
        }

        [HttpPost("downloadMultiple")]
        public IActionResult DownloadMultipleResumes([FromBody] List<int> ids)
        {
            var resumeUrls = _context.ResumesData
                .Where(r => ids.Contains(r.Id))
                .Select(r => Url.Action("DownloadResume", new { id = r.Id,}))
                .ToList();
            if(resumeUrls == null || !resumeUrls.Any())
            {
                return NotFound("No resume found");
            }
            return Ok(resumeUrls);
        }

      
        [HttpGet("id")]
        public async Task<IActionResult> GetProfileById(int id)
        {
            var resume = await _context.ResumesData.FirstOrDefaultAsync(x => x.Id == id);
            return Ok(resume);
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(" Invalid File Formate");
            }

            var containerName = "atsfilestorage";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // var tempFilePath = Path.GetTempFileName();
            var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);
            //    using (var Stream = file.OpenReadStream())

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                //await Stream.CopyToAsync(fileStream);
                await file.CopyToAsync(fileStream);
            }

            //change for Docx
            string content = string.Empty;

            if (Path.GetExtension(tempFilePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                content = ExtractTextFromPdf(tempFilePath);
            }
            else if (Path.GetExtension(tempFilePath).Equals(".doc", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetExtension(tempFilePath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            {
                content = ExtractTextFromWord(tempFilePath);
            }
            else
            {
                return BadRequest("Unsupported file format.");
            }



           // string content = ExtractTextFromPdf(tempFilePath);
            System.IO.File.Delete(tempFilePath);

            //var genAiService = HttpContext.RequestServices.GetRequiredService<GenAi_Service>();
            //var summary = await genAiService.SummarizeTextAsync(content);

            var resume = new Resume //ceate an object
            {
                FileName = file.FileName,
                FilePath = blobClient.Uri.ToString(),
                Content = content,
                Summary = "summary"
            };

            _context.ResumesData.Add(resume);
            await _context.SaveChangesAsync();

            return Ok("File uploaded and processed successfully.");
        }

        private string ExtractTextFromPdf(String filePath)
        {
            using (var pdf = PdfDocument.Open(filePath)) //opens the pdf document using file path
            {
                var text = new StringBuilder();
                foreach(var page in pdf.GetPages())
                {
                    text.Append(page.Text);
                }
                return text.ToString();
            }
        }

        private string ExtractTextFromWord(string filePath)
        {
            var document = new Aspose.Words.Document(filePath);
            return document.ToString(SaveFormat.Text);
        }


        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {

            if(string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("No search criteria provided");
            }

            //changes for {,/' '} adding
            var delimiters = new[] { ',', '/', ' ' };



            var searchTerms = query.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .ToArray();

            //var resumes = await _context.ResumesData
            //    .Where(BuildSearchExpression(searchTerms))
            //    .ToListAsync();

            var resumes = await _context.ResumesData
                .Where(BuildSearchExpression(searchTerms))
                .Select(r => new
                {
                    Id = r.Id,
                    FileName = r.FileName,
                    PdfUrl = Url.Action(nameof(DownloadResume), "ResumesData", new { id = r.Id }, Request.Scheme)
                }).ToListAsync();

            if (!resumes.Any())
                return NotFound("No MAching Resume found");
            return Ok(resumes);

        }
        private Expression<Func<Resume, bool>> BuildSearchExpression(string[] searchTerms)
        {
            if (searchTerms.Length == 0 || searchTerms == null)
            {
                return r => false;
            }

            //check resume and it`s content using LINQ Expression-parameter object and property-content
            var parameter = Expression.Parameter(typeof(Resume), "r");
            var property = Expression.Property(parameter, nameof(Resume.Content) );

            //ensure property is string type
            if(property.Type != typeof(string))
            {
                throw new InvalidOperationException($"Property {nameof(Resume.Content)} is not supported.");
            }

            //combines all the search expression
            Expression combined = null;

            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            //check if contains method exist
            
            if(containsMethod == null)
            {
                throw new InvalidOperationException("Unable to find Contains method");
            }

            foreach (var term in searchTerms)
            {
                var searchTerm = Expression.Constant(term);
                //var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var containsExpression = Expression.Call(property, containsMethod, searchTerm);

                if (combined == null)
                {
                    combined = containsExpression;
                }
                else
                {
                    //for Or logib

                    //combined = Expression.OrElse(combined, containsExpression);

                    //for AND logic
                    combined = Expression.AndAlso(combined, containsExpression);
                }
            }
            return Expression.Lambda<Func<Resume, bool>>(combined, parameter);
        }

        
        
    }

    
}
