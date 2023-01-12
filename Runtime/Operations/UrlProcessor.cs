using System;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.Core;

namespace ReadyPlayerMe.AvatarLoader
{
    /// <summary>
    /// This class is responsible for validating and processing the provided avatar model URL.
    /// </summary>
    public class UrlProcessor : IOperation<AvatarContext>
    {
        private const string TAG = nameof(UrlProcessor);

        private const string SHORT_CODE_BASE_URL = "https://api.readyplayer.me/v1/avatars";
        private const string GLB_EXTENSION = ".glb";
        private const string JSON_EXTENSION = ".json";

        private bool SaveInProjectFolder { get; set; }

        public int Timeout { get; set; }
        public Action<float> ProgressChanged { get; set; }

        /// <summary>
        /// Executes the operation validate and create the avatar URL.
        /// </summary>
        /// <param name="context">A container for all the data related to the Avatar model.</param>
        /// <param name="token">Can be used to cancel the operation.</param>
        /// <returns>The updated <see cref="AvatarContext" />.</returns>
        public async Task<AvatarContext> Execute(AvatarContext context, CancellationToken token)
        {
            if (string.IsNullOrEmpty(context.Url))
            {
                throw Fail(FailureType.UrlProcessError, "Url string is null");
            }

            SaveInProjectFolder = context.SaveInProjectFolder;
            try
            {
                context.AvatarUri = await ProcessUrl(context.Url, context.ParametersHash, token);
            }
            catch (Exception e)
            {
                throw Fail(FailureType.UrlProcessError, $"Invalid avatar URL or short code.{e.Message}");
            }

            ProgressChanged?.Invoke(1);
            return context;
        }

        /// <summary>
        /// This method generates all the required avatar model URL information and returns it in a <see cref="AvatarUri" />.
        /// </summary>
        /// <param name="url">The avatar model URL.</param>
        /// <param name="paramsHash">This parameter hash is used organize the locally stored files for avatar caching.</param>
        /// <param name="token">Can be used to cancel the operation.</param>
        /// <returns>The avatar model URL and path information as a <see cref="AvatarUri" />.</returns>
        public async Task<AvatarUri> ProcessUrl(string url, string paramsHash, CancellationToken token = new CancellationToken())
        {
            var fractions = url.Split('?'); // separate parameters
            url = fractions[0];
            var avatarApiParameters = fractions.Length > 1 ? $"?{fractions[1]}" : "";
            if (url.ToLower().EndsWith(GLB_EXTENSION))
            {
                return CreateUri(url, paramsHash, avatarApiParameters).Result;
            }

            var urlFromShortCode = await GetUrlFromShortCode(url);
            return CreateUri(urlFromShortCode, paramsHash, avatarApiParameters).Result;
        }

        /// <summary>
        /// Creates a URI from the <paramref name="url" />, <paramref name="paramHash" /> and
        /// <paramref name="avatarApiParameters" />.
        /// </summary>
        /// <param name="url">The avatar model url.</param>
        /// <param name="paramsHash">This parameter hash is used organize the locally stored files for avatar caching.</param>
        /// <param name="avatarApiParameters">The combined avatar api parameters as a <c>string</c>.</param>
        /// <returns>The avatar model URL and path information as a <see cref="AvatarUri" />.</returns>
        private Task<AvatarUri> CreateUri(string url, string paramsHash, string avatarApiParameters)
        {
            try
            {
                var avatarUri = new AvatarUri();

                var fractions = url.Split('/', '.');

                avatarUri.Guid = fractions[fractions.Length - 2];
                var fileName = $"{DirectoryUtility.GetAvatarSaveDirectory(avatarUri.Guid, SaveInProjectFolder, paramsHash)}/{avatarUri.Guid}";
                avatarUri.ModelUrl = $"{url}{avatarApiParameters}";
                avatarUri.LocalModelPath = $"{fileName}{GLB_EXTENSION}";

                url = url.Remove(url.Length - GLB_EXTENSION.Length, GLB_EXTENSION.Length);
                avatarUri.MetadataUrl = $"{url}{JSON_EXTENSION}";
                fileName = $"{DirectoryUtility.GetAvatarSaveDirectory(avatarUri.Guid, SaveInProjectFolder)}/{avatarUri.Guid}";
                avatarUri.LocalMetadataPath = $"{fileName}{JSON_EXTENSION}";

                SDKLogger.Log(TAG, "Processing completed.");
                return Task.FromResult(avatarUri);
            }
            catch (Exception e)
            {
                throw Fail(FailureType.UrlProcessError, $"Invalid avatar URL. {e.Message}");
            }
        }

        /// <summary>
        /// This method builds a URL from the provided shortCode.
        /// </summary>
        /// <param name="shortCode">The avatar shortcode.</param>
        /// <returns>A URL as a <c>string</c>.</returns>
        private Task<string> GetUrlFromShortCode(string shortCode)
        {
            var url = $"{SHORT_CODE_BASE_URL}/{shortCode}{GLB_EXTENSION}";
            return Task.FromResult(url);
        }

        /// <summary>
        /// A method used to throw <see cref="CustomException" /> exceptions.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>
        /// The <<see cref="Exception" />.
        /// </returns>
        private Exception Fail(FailureType failureType, string message)
        {
            SDKLogger.Log(TAG, message);
            throw new CustomException(failureType, message);
        }
    }
}
