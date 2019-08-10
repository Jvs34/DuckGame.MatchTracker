using Octokit;
using Octokit.Internal;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	//copied from their own implementation except that I pass in my prefered http client
	internal class OctoKitHttpClient : IHttpClient
	{
		readonly HttpClient _http;

		public const string RedirectCountKey = "RedirectCount";

		public OctoKitHttpClient( HttpClient client )
		{
			_http = client;
		}

		public async Task<IResponse> Send( IRequest request , CancellationToken cancellationToken )
		{
			var cancellationTokenForRequest = GetCancellationTokenForRequest( request , cancellationToken );

			using( var requestMessage = BuildRequestMessage( request ) )
			{
				var responseMessage = await SendAsync( requestMessage , cancellationTokenForRequest ).ConfigureAwait( false );

				return await BuildResponse( responseMessage ).ConfigureAwait( false );
			}
		}

		static CancellationToken GetCancellationTokenForRequest( IRequest request , CancellationToken cancellationToken )
		{
			var cancellationTokenForRequest = cancellationToken;

			if( request.Timeout != TimeSpan.Zero )
			{
				var timeoutCancellation = new CancellationTokenSource( request.Timeout );
				var unifiedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken , timeoutCancellation.Token );

				cancellationTokenForRequest = unifiedCancellationToken.Token;
			}
			return cancellationTokenForRequest;
		}

		protected virtual async Task<IResponse> BuildResponse( HttpResponseMessage responseMessage )
		{
			object responseBody = null;
			string contentType = null;

			// We added support for downloading images,zip-files and application/octet-stream. 
			// Let's constrain this appropriately.
			var binaryContentTypes = new [] {
				"application/zip" ,
				"application/x-gzip" ,
				"application/octet-stream"};

			using( var content = responseMessage.Content )
			{
				if( content != null )
				{
					contentType = GetContentMediaType( responseMessage.Content );

					if( contentType != null && ( contentType.StartsWith( "image/" ) || binaryContentTypes
						.Any( item => item.Equals( contentType , StringComparison.OrdinalIgnoreCase ) ) ) )
					{
						responseBody = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait( false );
					}
					else
					{
						responseBody = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait( false );
					}
				}
			}

			return new Response(
				responseMessage.StatusCode ,
				responseBody ,
				responseMessage.Headers.ToDictionary( h => h.Key , h => h.Value.First() ) ,
				contentType );
		}

		protected virtual HttpRequestMessage BuildRequestMessage( IRequest request )
		{
			HttpRequestMessage requestMessage = null;
			try
			{
				var fullUri = new Uri( request.BaseAddress , request.Endpoint );
				requestMessage = new HttpRequestMessage( request.Method , fullUri );

				foreach( var header in request.Headers )
				{
					requestMessage.Headers.Add( header.Key , header.Value );
				}
				var httpContent = request.Body as HttpContent;
				if( httpContent != null )
				{
					requestMessage.Content = httpContent;
				}

				var body = request.Body as string;
				if( body != null )
				{
					requestMessage.Content = new StringContent( body , Encoding.UTF8 , request.ContentType );
				}

				var bodyStream = request.Body as Stream;
				if( bodyStream != null )
				{
					requestMessage.Content = new StreamContent( bodyStream );
					requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue( request.ContentType );
				}
			}
			catch( Exception )
			{
				if( requestMessage != null )
				{
					requestMessage.Dispose();
				}
				throw;
			}

			return requestMessage;
		}

		static string GetContentMediaType( HttpContent httpContent )
		{
			if( httpContent.Headers?.ContentType != null )
			{
				return httpContent.Headers.ContentType.MediaType;
			}
			return null;
		}

		public void Dispose()
		{
		}

		protected virtual void Dispose( bool disposing )
		{
		}

		public async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request , CancellationToken cancellationToken )
		{
			// Clone the request/content incase we get a redirect
			var clonedRequest = await CloneHttpRequestMessageAsync( request ).ConfigureAwait( false );

			// Send initial response
			var response = await _http.SendAsync( request , HttpCompletionOption.ResponseContentRead , cancellationToken ).ConfigureAwait( false );

			// Can't redirect without somewhere to redirect to.
			if( response.Headers.Location == null )
			{
				return response;
			}

			// Don't redirect if we exceed max number of redirects
			var redirectCount = 0;
			if( request.Properties.Keys.Contains( RedirectCountKey ) )
			{
				redirectCount = (int) request.Properties [RedirectCountKey];
			}

			if( redirectCount > 3 )
			{
				throw new InvalidOperationException( "The redirect count for this request has been exceeded. Aborting." );
			}

			if( response.StatusCode == HttpStatusCode.MovedPermanently
						|| response.StatusCode == HttpStatusCode.Redirect
						|| response.StatusCode == HttpStatusCode.Found
						|| response.StatusCode == HttpStatusCode.SeeOther
						|| response.StatusCode == HttpStatusCode.TemporaryRedirect
						|| (int) response.StatusCode == 308 )
			{
				if( response.StatusCode == HttpStatusCode.SeeOther )
				{
					clonedRequest.Content = null;
					clonedRequest.Method = HttpMethod.Get;
				}

				// Increment the redirect count
				clonedRequest.Properties [RedirectCountKey] = ++redirectCount;

				// Set the new Uri based on location header
				clonedRequest.RequestUri = response.Headers.Location;

				// Clear authentication if redirected to a different host
				if( string.Compare( clonedRequest.RequestUri.Host , request.RequestUri.Host , StringComparison.OrdinalIgnoreCase ) != 0 )
				{
					clonedRequest.Headers.Authorization = null;
				}

				// Send redirected request
				response = await SendAsync( clonedRequest , cancellationToken ).ConfigureAwait( false );
			}

			return response;
		}

		public static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync( HttpRequestMessage oldRequest )
		{
			var newRequest = new HttpRequestMessage( oldRequest.Method , oldRequest.RequestUri );

			// Copy the request's content (via a MemoryStream) into the cloned object
			var ms = new MemoryStream();
			if( oldRequest.Content != null )
			{
				await oldRequest.Content.CopyToAsync( ms ).ConfigureAwait( false );
				ms.Position = 0;
				newRequest.Content = new StreamContent( ms );

				// Copy the content headers
				if( oldRequest.Content.Headers != null )
				{
					foreach( var h in oldRequest.Content.Headers )
					{
						newRequest.Content.Headers.Add( h.Key , h.Value );
					}
				}
			}

			newRequest.Version = oldRequest.Version;

			foreach( var header in oldRequest.Headers )
			{
				newRequest.Headers.TryAddWithoutValidation( header.Key , header.Value );
			}

			foreach( var property in oldRequest.Properties )
			{
				newRequest.Properties.Add( property );
			}

			return newRequest;
		}


		public void SetRequestTimeout( TimeSpan timeout )
		{
		}
	}
}
