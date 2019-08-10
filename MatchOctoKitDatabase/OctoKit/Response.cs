using Octokit;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace MatchTracker
{
	internal class Response : IResponse
	{
		public object Body { get; private set; }
		public IReadOnlyDictionary<string , string> Headers { get; private set; }
		public ApiInfo ApiInfo { get; internal set; }
		public HttpStatusCode StatusCode { get; private set; }
		public string ContentType { get; private set; }


		public Response() : this( new Dictionary<string , string>() )
		{

		}

		public Response( IDictionary<string , string> headers )
		{
			Headers = new ReadOnlyDictionary<string , string>( headers );
			ApiInfo = ApiInfoParser.ParseResponseHeaders( headers );
		}

		public Response( HttpStatusCode statusCode , object body , IDictionary<string , string> headers , string contentType )
		{
			StatusCode = statusCode;
			Body = body;
			Headers = new ReadOnlyDictionary<string , string>( headers );
			ApiInfo = ApiInfoParser.ParseResponseHeaders( headers );
			ContentType = contentType;
		}
	}
}
