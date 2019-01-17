namespace MovieLensQL.Server.Data

#nowarn "40"

open FSharp.Data.GraphQL.Types
open System.Data.SqlClient
open Microsoft.EntityFrameworkCore

type Root =
    { RequestId : string }

[<CLIMutable>]
type Movie =
    { MovieId : int
      Genres : string
      Title : string }

[<CLIMutable>]
type Rating =
    { MovieId : int
      UserId : int
      Rating : decimal
      Timestamp : int }

[<CLIMutable>]
type Link =
    { MovieId : int
      ImdbId : int
      TmdbId : System.Nullable<int> }

[<CLIMutable>]
type Tag =
    { TagId : int
      MovieId : int
      UserId : int 
      Tag : string
      Timestamp : int }

[<CLIMutable>]
type GenomeTag =
    { TagId : int
      Tag : string }

[<CLIMutable>]
type GenomeScore =
    { MovieId : int
      TagId : int
      Relevance : decimal }

type MovieDbContext(options : DbContextOptions<MovieDbContext>) =
    inherit DbContext(options)

    override __.OnModelCreating(builder) =
        builder.Entity<Movie>().HasKey("MovieId") |> ignore
        builder.Entity<Link>().HasKey("MovieId") |> ignore
        builder.Entity<Tag>().HasKey("TagId") |> ignore
        builder.Entity<GenomeTag>().HasKey("TagId") |> ignore
        builder.Entity<GenomeScore>().HasKey("MovieId", "TagId") |> ignore
        builder.Entity<Rating>().HasKey("MovieId", "UserId") |> ignore

    [<DefaultValue>]
    val mutable private movies : DbSet<Movie>
    abstract member Movies : DbSet<Movie> with get, set
    default this.Movies 
        with get() = this.movies
        and set(value) = this.movies <- value

    [<DefaultValue>]
    val mutable private ratings : DbSet<Rating>
    abstract member Ratings : DbSet<Rating> with get, set
    default this.Ratings
        with get() = this.ratings
        and set(value) = this.ratings <- value

    [<DefaultValue>]
    val mutable private links : DbSet<Link>
    abstract member Links : DbSet<Link> with get, set
    default this.Links
        with get() = this.links
        and set(value) = this.links <- value

    [<DefaultValue>]
    val mutable private tags : DbSet<Tag>
    abstract member Tags : DbSet<Tag> with get, set
    default this.Tags
        with get() = this.tags
        and set(value) = this.tags <- value

    [<DefaultValue>]
    val mutable private genomeTags : DbSet<GenomeTag>
    abstract member GenomeTags : DbSet<GenomeTag> with get, set
    default this.GenomeTags
        with get() = this.genomeTags
        and set(value) = this.genomeTags <- value

    [<DefaultValue>]
    val mutable private genomeScores : DbSet<GenomeScore>
    abstract member GenomeScores : DbSet<GenomeScore> with get, set
    default this.GenomeScores
        with get() = this.genomeScores
        and set(value) = this.genomeScores <- value

module SchemaDefinition =
    let private context = 
        let options = 
            let connection = new SqlConnection("Data Source=.;Initial Catalog=MovieLens;Integrated Security=True")
            let builder = DbContextOptionsBuilder<MovieDbContext>()
            builder.UseSqlServer(connection).Options
        new MovieDbContext(options)

    let rec Rating =
        Define.Object(
            name = "Rating",
            isTypeOf = (fun o -> o :? Rating),
            fieldsFn = fun () ->
                [ Define.Field("movieId", Int, resolve = fun _ (r : Rating) -> r.MovieId)
                  Define.AsyncField("movie", Movie, resolve = fun _ (r : Rating) -> 
                    query {
                        for movie in context.Movies do
                        find (r.MovieId = movie.MovieId) } 
                    |> async.Return)
                  Define.Field("userId", Int, resolve = fun _ (r : Rating) -> r.UserId)
                  Define.Field("rating", Float, resolve = fun _ (r : Rating) -> System.Convert.ToDouble(r.Rating))
                  Define.Field("timestamp", Int, resolve = fun _ (r : Rating) -> r.Timestamp) ])

    and Link =
        Define.Object(
            name = "Link",
            isTypeOf = (fun o -> o :? Link),
            fieldsFn = fun () ->
                [ Define.Field("movieId", Int, resolve = fun _ (l : Link) -> l.MovieId)
                  Define.AsyncField("movie", Movie, resolve = fun _ (l : Link) -> 
                    query {
                        for movie in context.Movies do
                        find (l.MovieId = movie.MovieId) }
                    |> async.Return)
                  Define.Field("imdbId", Int, resolve = fun _ (l : Link) -> l.ImdbId)
                  Define.Field("tmdbId", Nullable Int, resolve = fun _ (l : Link) -> l.TmdbId |> Option.ofNullable) ])

    and Tag =
        Define.Object(
            name = "Tag",
            isTypeOf = (fun o -> o :? Tag),
            fieldsFn = fun () ->
                [ Define.Field("tagId", Int, resolve = fun _ (t : Tag) -> t.TagId)
                  Define.Field("movieId", Int, resolve = fun _ (t : Tag) -> t.MovieId)
                  Define.AsyncField("movie", Movie, resolve = fun _ (t : Tag) -> 
                    query {
                        for movie in context.Movies do
                        find (t.MovieId = movie.MovieId) }
                    |> async.Return)
                  Define.Field("userId", Int, resolve = fun _ (t : Tag) -> t.UserId)
                  Define.Field("timestamp", Int, resolve = fun _ (t : Tag) -> t.Timestamp) ])

    and GenomeTag =
        Define.Object(
            name = "GenomeTag",
            isTypeOf = (fun o -> o :? GenomeTag),
            fieldsFn = fun () ->
                [ Define.Field("tagId", Int, resolve = fun _ (gn : GenomeTag) -> gn.TagId)
                  Define.AsyncField("tag", Tag, resolve = fun _ (gn : GenomeTag) -> 
                    query {
                        for tag in context.Tags do
                        find (gn.TagId = tag.TagId) }
                    |> async.Return)
                  Define.Field("value", String, resolve = fun _ (gn : GenomeTag) -> gn.Tag) ])

    and GenomeScore =
        Define.Object(
            name = "GenomeScore",
            isTypeOf = (fun o -> o :? GenomeScore),
            fieldsFn = fun () ->
                [ Define.Field("movieId", Int, resolve = fun _ (gs : GenomeScore) -> gs.MovieId)
                  Define.AsyncField("movie", Movie, resolve = fun _ (gs : GenomeScore) -> 
                    query {
                        for movie in context.Movies do
                        find (gs.MovieId = movie.MovieId) }
                    |> async.Return)
                  Define.Field("tagId", Int, resolve = fun _ (gs : GenomeScore) -> gs.TagId)
                  Define.AsyncField("tag", Tag, resolve = fun _ (gs : GenomeScore) -> 
                    query {
                        for tag in context.Tags do
                        find (gs.TagId = tag.TagId) }
                    |> async.Return)
                  Define.Field("relevance", Float, resolve = fun _ (gs : GenomeScore) -> System.Convert.ToDouble(gs.Relevance)) ])

    and Movie =
        Define.Object(
            name = "Movie",
            isTypeOf = (fun o -> o :? Movie),
            fieldsFn = fun () ->
                [ Define.Field("movieId", Int, resolve = fun _ (m : Movie) -> m.MovieId)
                  Define.Field("title", String, resolve = fun _ (m : Movie) -> m.Title)
                  Define.Field("genres", ListOf String, resolve = fun _ (m : Movie) -> m.Genres.Split('|'))
                  Define.AsyncField(
                    "ratings", 
                    ListOf Rating,
                    "Gets movie ratings",
                    args = [ Define.Input("userId", Nullable Int) ],
                    resolve = fun ctx (m : Movie) -> 
                        let ratings = query {
                            for rating in context.Ratings do
                            where (rating.MovieId = m.MovieId)
                            select rating }
                        match ctx.TryArg("userId") with
                        | Some (Some userId) -> 
                            query {
                                for rating in ratings do
                                where (rating.UserId = userId)
                                select rating }
                            |> async.Return
                        | _ -> ratings |> async.Return)
                  Define.AsyncField(
                    "links", 
                    ListOf Link,
                    "Gets movie links",
                    resolve = fun _ (m : Movie) -> 
                        query {
                            for link in context.Links do
                            where (link.MovieId = m.MovieId)
                            select link }
                        |> async.Return)
                  Define.AsyncField(
                    "tags", 
                    ListOf Tag, 
                    "Gets movie tags",
                    args = [ Define.Input("userId", Nullable Int) ],
                    resolve = fun ctx (m : Movie) -> 
                        let tags = query {
                            for tag in context.Tags do
                            where (tag.MovieId = m.MovieId)
                            select tag }
                        match ctx.TryArg("userId") with
                        | Some (Some userId) -> 
                            query {
                                for tag in tags do
                                where (tag.UserId = userId)
                                select tag }
                            |> async.Return
                        | _ -> tags |> async.Return)
                  Define.AsyncField(
                    "genomeScores", 
                    ListOf GenomeScore,
                    "Gets movie genome scores",
                    args = [ Define.Input("tagId", Nullable Int) ],
                    resolve = fun ctx (m : Movie) -> 
                        let scores = query {
                            for score in context.GenomeScores do
                            where (score.MovieId = m.MovieId)
                            select score }
                        match ctx.TryArg("tagId") with
                        | Some (Some tagId) -> 
                            query { 
                                for score in scores do
                                where (score.TagId = tagId) 
                                select score }
                            |> async.Return
                        | _ -> scores |> async.Return) ])
    let Query = 
        Define.Object<Root>(
            name = "Query",
            fields = [
                Define.Field(
                    "requestId",
                    String,
                    resolve = fun _ (r : Root) -> r.RequestId)
                Define.AsyncField(
                    "movie", 
                    Nullable Movie, 
                    "Gets movie by it's id",
                    args = [ Define.Input("movieId", Int) ], 
                    resolve = fun ctx _ -> 
                        query {
                            for movie in context.Movies do
                            where (movie.MovieId = ctx.Arg("movieId"))
                            select (Some movie)
                            headOrDefault }
                        |> async.Return) ])

module Schema =
    open FSharp.Data.GraphQL

    let config = SchemaConfig.Default

    let root requestId = { RequestId = requestId }

    let instance = Schema(SchemaDefinition.Query, config = config)

    let executor = Executor(instance)