﻿using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PasApiClient
{
    /// <summary>Client component for accessing Parser as a Service.</summary>
    public class ParserClient
    {
        #region Inner Types
        private class SingleParsingInput
        {
            public string Grammar { get; set; }

            public string Rule { get; set; }

            public string Text { get; set; }
        }
        #endregion

        private static readonly Uri DEFAULT_BASE_URI = new Uri("http://pas-api.vplauzon.com");
        private readonly Uri _baseUri;

        #region Constructors
        public static ParserClient Create()
        {
            return CreateFromBaseUri(DEFAULT_BASE_URI);
        }

        public static ParserClient CreateFromBaseUri(Uri baseUri)
        {
            return new ParserClient(baseUri);
        }

        private ParserClient(Uri baseUri)
        {
            _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        }
        #endregion

        #region Single
        public Task<ParsingResult> SingleParseAsync(string grammar, string text)
        {
            return SingleParseAsync(grammar, null, text);
        }

        public async Task<ParsingResult> SingleParseAsync(string grammar, string rule, string text)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(_baseUri, new Uri("v1/single", UriKind.Relative));
                var input = new SingleParsingInput
                {
                    Grammar = grammar,
                    Rule = rule,
                    Text = text
                };
                var inputString = JsonConvert.SerializeObject(input);
                var content = new StringContent(inputString, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, content);
                var outputString = await response.Content.ReadAsStringAsync();
                var output = JsonConvert.DeserializeObject<ParsingResult>(outputString);

                return output;
            }

            throw new NotImplementedException();
        }
        #endregion

        #region Multiple
        #endregion
    }
}