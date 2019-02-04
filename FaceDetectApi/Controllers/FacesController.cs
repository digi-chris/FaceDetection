using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Emgu.CV;

namespace FaceDetectApi.Controllers
{
    [Route("api/faces")]
    public class FacesController : ApiController
    {
        [HttpPost()]
        public async Task<HttpResponseMessage> Index()
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            Emgu.CV.CascadeClassifier cc = new Emgu.CV.CascadeClassifier(System.Web.Hosting.HostingEnvironment.MapPath("/haarcascade_frontalface_alt_tree.xml"));
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.Contents)
            {
                var filename = file.Headers.ContentDisposition.FileName.Trim('\"');
                var buffer = await file.ReadAsByteArrayAsync();
                using (MemoryStream mStream = new MemoryStream(buffer, 0, buffer.Length))
                {
                    mStream.Position = 0;
                    //Do whatever you want with filename and its binary data.

                    using (Bitmap bmp = new Bitmap(mStream))
                    {
                        using (Emgu.CV.Image<Emgu.CV.Structure.Bgr, Int32> img = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, Int32>(bmp))
                        {
                            if (img != null)
                            {
                                var grayframe = img.Convert<Emgu.CV.Structure.Gray, byte>();
                                var faces = cc.DetectMultiScale(grayframe);//, 1.1, 10, Size.Empty);
                                int faceCount = 0;
                                foreach (var face in faces)
                                {
                                    // only returns the first face found
                                    faceCount++;
                                    using (Bitmap faceBmp = new Bitmap(face.Right - face.Left, face.Bottom - face.Top))
                                    {
                                        Graphics g = Graphics.FromImage(faceBmp);
                                        g.DrawImage(bmp, new Rectangle(0, 0, faceBmp.Width, faceBmp.Height), face.Left, face.Top, faceBmp.Width, faceBmp.Height, GraphicsUnit.Pixel);
                                        MemoryStream outStream = new MemoryStream();
                                        faceBmp.Save(outStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        var result = new HttpResponseMessage(HttpStatusCode.OK)
                                        {
                                            Content = new ByteArrayContent(outStream.ToArray()),
                                        };
                                        result.Content.Headers.ContentDisposition =
                                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                                        {
                                            FileName = "face.jpg"
                                        };
                                        result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                                        result.Content.Headers.ContentLength = outStream.Length;

                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }
    }
}
