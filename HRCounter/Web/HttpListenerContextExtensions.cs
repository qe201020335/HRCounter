﻿using System.Net;

namespace HRCounter.Web
{
    public static class HttpListenerContextExtensions
    {
        private static void SimpleStatusCodeResponse(this HttpListenerContext context, HttpStatusCode statusCode)
        {
            var response = context.Response;
            response.StatusCode = (int)statusCode;
            response.Close();
        }

        public static void BadMethod(this HttpListenerContext context)
        {
            context.SimpleStatusCodeResponse(HttpStatusCode.MethodNotAllowed);
        }

        public static void BadRequest(this HttpListenerContext context)
        {
            context.SimpleStatusCodeResponse(HttpStatusCode.BadRequest);
        }

        public static void NotFound(this HttpListenerContext context)
        {
            context.SimpleStatusCodeResponse(HttpStatusCode.NotFound);
        }

        public static void InternalServerError(this HttpListenerContext context, object? body = null)
        {
            if (body != null)
            {
                context.SendResponse(body.ToString(), code: HttpStatusCode.InternalServerError);
            }
            else
            {
                context.SimpleStatusCodeResponse(HttpStatusCode.InternalServerError);
            }
        }

        public static void SendResponse(this HttpListenerContext context, string text, string contentType = "text/plain", HttpStatusCode code = HttpStatusCode.OK)
        {
            var response = context.Response;
            response.StatusCode = (int)code;
            response.ContentType = contentType;
            response.ContentEncoding = System.Text.Encoding.UTF8;
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        public static void SendJsonResponse(this HttpListenerContext context, string json)
        {
            context.SendResponse(json, contentType: "application/json");
        }
    }
}