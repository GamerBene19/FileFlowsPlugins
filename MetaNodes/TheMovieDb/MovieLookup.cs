﻿namespace MetaNodes.TheMovieDb
{
    using System.Text.RegularExpressions;
    using DM.MovieApi;
    using DM.MovieApi.ApiResponse;
    using DM.MovieApi.MovieDb.Movies;
    using FileFlows.Plugin;
    using FileFlows.Plugin.Attributes;

    public class MovieLookup : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 2;

        public override FlowElementType Type => FlowElementType.Logic;

        public override string Icon => "fas fa-film";

        private Dictionary<string, object> _Variables;

        public override Dictionary<string, object> Variables => _Variables;

        public MovieLookup()
        {
            _Variables = new Dictionary<string, object>()
            {
                { "movie.Title", "Batman Begins" },
                { "movie.Year", 2005 }
            };
        }

        internal const string MovieDbBearerToken = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIxZjVlNTAyNmJkMDM4YmZjZmU2MjI2MWU2ZGEwNjM0ZiIsInN1YiI6IjRiYzg4OTJjMDE3YTNjMGY5MjAwMDIyZCIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.yMwyT8DEK1rF1gQMKJ-ZSy-dUGxFs5T345XwBLrvrWE";

        [Boolean(1)]
        public bool UseFolderName { get; set; }

        public override int Execute(NodeParameters args)
        {
            var fileInfo = new FileInfo(args.FileName);
            string lookupName = UseFolderName ? fileInfo.Directory.Name : fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf(fileInfo.Extension));
            lookupName = lookupName.Replace(".", " ").Replace("_", " ");

            // look for year
            string year = string.Empty;
            var match = Regex.Matches(lookupName, @"((19[2-9][0-9])|(20[0-9]{2}))(?=([\.\s_\-\)\]]|$))").LastOrDefault();
            if (match != null)
            {
                year = match.Value;
                lookupName = lookupName.Substring(0, lookupName.IndexOf(year)).Trim();
            }

            // remove double spaces incase they were added when removing the year
            while (lookupName.IndexOf("  ") > 0)
                lookupName = lookupName.Replace("  ", " ");

            // RegisterSettings only needs to be called one time when your application starts-up.
            MovieDbFactory.RegisterSettings(MovieDbBearerToken);

            var movieApi = MovieDbFactory.Create<IApiMovieRequest>().Value;

            ApiSearchResponse<MovieInfo> response = movieApi.SearchByTitleAsync(lookupName).Result;

            // try find an exact match
            var result = response.Results.OrderBy(x =>
                {
                    if (string.IsNullOrEmpty(year) == false)
                    {
                        return year == x.ReleaseDate.Year.ToString() ? 0 : 1;
                    }
                    return 0;
                })
                .ThenBy(x => x.Title.ToLower().Trim().Replace(" ", "") == lookupName.ToLower().Trim().Replace(" ", "") ? 0 : 1)
                .ThenBy(x =>
                {
                    // do some fuzzy logic with roman numerals
                    var numMatch = Regex.Match(lookupName, @"[\s]([\d]+)$");
                    if (numMatch.Success == false)
                        return 0;
                    int number = int.Parse(numMatch.Groups[1].Value);
                    string roman = number switch
                    {
                        1 => "i",
                        2 => "ii",
                        3 => "iii",
                        4 => "iv",
                        5 => "v",
                        6 => "vi,",
                        7 => "vii",
                        8 => "viii",
                        9 => "ix",
                        10 => "x",
                        11 => "xi",
                        12 => "xii",
                        13 => "xiii",
                        _ => string.Empty
                    };
                    string ln = lookupName.Substring(0, lookupName.LastIndexOf(number.ToString())).ToLower().Trim().Replace(" ", "");
                    string softTitle = x.Title.ToLower().Replace(" ", "").Trim();
                    if (softTitle == ln + roman)
                        return 0;
                    if (softTitle.StartsWith(ln) && softTitle.EndsWith(roman))
                        return 0;
                    return 1;
                 })
                .ThenBy(x => lookupName.ToLower().Trim().Replace(" ", "").StartsWith(x.Title.ToLower().Trim().Replace(" ", "")) ? 0 : 1)
                .ThenBy(x => x.Title)
                .FirstOrDefault();

            if (result == null)
                return 2; // no match

            args.SetParameter(Globals.MOVIE_INFO, result);

            Variables["movie.Title"] = result.Title;
            Variables["movie.Year"] = result.ReleaseDate.Year;
            Variables["VideoMetadata"] = GetVideoMetadata(movieApi, result.Id, args.TempPath);

            args.UpdateVariables(Variables);
            return 1;

        }


        internal static VideoMetadata GetVideoMetadata(IApiMovieRequest movieApi, int id, string tempPath)
        {
            var movie = movieApi.FindByIdAsync(id).Result?.Item;
            if (movie == null)
                return null;
            
            var credits = movieApi.GetCreditsAsync(id).Result?.Item;

            VideoMetadata md = new();
            md.Title = movie.Title;
            md.Genres = movie.Genres?.Select(x => x.Name).ToList();
            md.Description = movie.Overview;
            md.Year = movie.ReleaseDate.Year;
            md.Subtitle = movie.Tagline;
            md.ReleaseDate = movie.ReleaseDate;
            if (string.IsNullOrWhiteSpace(movie.PosterPath) == false)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    using var stream = httpClient.GetStreamAsync("https://image.tmdb.org/t/p/w500" + movie.PosterPath).Result;
                    string file = Path.Combine(tempPath, Guid.NewGuid() + ".jpg");
                    using var fileStream = new FileStream(file, FileMode.CreateNew);
                    stream.CopyTo(fileStream);
                    md.ArtJpeg = file;
                }
                catch (Exception)
                {

                }
            }
            
            if(credits != null)
            {
                md.Actors = credits.CastMembers?.Select(x => x.Name)?.ToList();
                md.Writers  = credits.CrewMembers?.Where(x => x.Job == "Writer" || x.Job == "Screenplay") ?.Select(x => x.Name)?.ToList();
                md.Directors = credits.CrewMembers?.Where(x => x.Job == "Director")?.Select(x => x.Name)?.ToList();
                md.Producers = credits.CrewMembers?.Where(x => x.Job == "Producer")?.Select(x => x.Name)?.ToList();
            }

            return md;
        }
    }
}