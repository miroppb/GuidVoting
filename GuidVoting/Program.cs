using Dapper;
using Dapper.Contrib.Extensions;
using GuidVoting.Models;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();

// MySQL Connection
builder.Services.AddScoped<IDbConnection>(sp => GuidVoting.Secrets.GetConnectionString());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// Serve Admin Page
app.MapGet("/admin", async context =>
{
	context.Response.ContentType = "text/html";
	await context.Response.SendFileAsync("wwwroot/admin.html");
});

app.MapGet("/vote/{eventId:int}/{guid:guid}", async (HttpContext context) =>
{
	var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vote.html");
	if (File.Exists(filePath))
	{
		context.Response.ContentType = "text/html";
		await context.Response.SendFileAsync(filePath);
	}
	else
	{
		context.Response.StatusCode = StatusCodes.Status404NotFound;
		await context.Response.WriteAsync("Vote page not found");
	}
});

app.MapGet("/vote/check/{eventId:int}/{guid:guid}", async ([FromServices] IDbConnection db, HttpContext context, int eventId, Guid guid) =>
{
	context.Response.ContentType = "application/json";

	// Check if eventId exists and get the event name
	var eventName = await db.QueryFirstOrDefaultAsync<string>("SELECT Name FROM events WHERE Id = @EventId", new { EventId = eventId });

	if (eventName == null)
	{
		await context.Response.WriteAsJsonAsync(new { success = false, message = "Error: Event not found" });
		return;
	}

	// Check if GUID has already been used
	var guidUsed = await db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(1) FROM vote_guids WHERE guid = @Guid", new { Guid = guid }) > 0;

	if (guidUsed)
	{
		await context.Response.WriteAsJsonAsync(new { success = false, message = "Error: GUID has already been used" });
		return;
	}

	// If both checks pass, return success with event name
	await context.Response.WriteAsJsonAsync(new { success = true, eventName = eventName });
});

// Create a new event
app.MapPost("/admin/events", async ([FromServices] IDbConnection db, Event newEvent) =>
{
	try
	{
		var id = await db.InsertAsync(newEvent);
		return Results.Created($"/admin/events/{id}", newEvent);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error inserting event: {ex.Message}");
		return Results.Problem("Failed to create event.");
	}
});

// Get all events
app.MapGet("/admin/events", async ([FromServices] IDbConnection db) =>
{
	var events = await db.GetAllAsync<Event>();
	return Results.Ok(events);
});

// Get questions for a specific event
app.MapGet("/admin/events/{eventId}/questions", async ([FromServices] IDbConnection db, int eventId) =>
{
	var questions = await db.QueryAsync<Question>("SELECT * FROM questions WHERE EventId = @EventId", new { EventId = eventId });
	return Results.Ok(questions);
});

// Add a question to a specific event
app.MapPost("/admin/events/{eventId}/questions", async ([FromServices] IDbConnection db, int eventId, Question question) =>
{
	question.EventId = eventId;
	var id = await db.InsertAsync(question);
	return Results.Created($"/admin/events/{eventId}/questions/{id}", question);
});

// Add Choice to a Question
app.MapPost("/admin/questions/{questionId}/choices", async ([FromServices] IDbConnection db, int questionId, Choice choice) =>
{
	try
	{
		choice.QuestionId = questionId;
		var id = await db.InsertAsync(choice);
		return Results.Created($"/admin/questions/{questionId}/choices/{id}", choice);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error adding choice: {ex.Message}");
		return Results.Problem("Failed to add choice.");
	}
});

// Get Choices for a Question
app.MapGet("/admin/questions/{questionId}/choices", async ([FromServices] IDbConnection db, int questionId) =>
{
	var choices = await db.QueryAsync<Choice>("SELECT * FROM choices WHERE QuestionId = @QuestionId", new { QuestionId = questionId });
	return Results.Ok(choices);
});

// Update a choice by ID
app.MapPut("/admin/choices/{choiceId}", async ([FromServices] IDbConnection db, int choiceId, [FromBody] Choice choice) =>
{
	var existingChoice = await db.GetAsync<Choice>(choiceId);
	if (existingChoice == null) return Results.NotFound();

	existingChoice.ChoiceText = choice.ChoiceText;
	await db.UpdateAsync(existingChoice);
	return Results.NoContent();
});

// Delete a question
app.MapDelete("/admin/questions/{questionId}", async ([FromServices] IDbConnection db, int questionId) =>
{
	try
	{
		var affectedRows = await db.ExecuteAsync("DELETE FROM questions WHERE Id = @Id", new { Id = questionId });
		if (affectedRows > 0)
		{
			return Results.NoContent();
		}
		return Results.NotFound();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error deleting question: {ex.Message}");
		return Results.Problem("Failed to delete question.");
	}
});

app.MapGet("/admin/events/{eventId}/generate-pdf", async ([FromServices] IDbConnection db, int eventId, int count) =>
{
	var questions = await db.QueryAsync<Question>("SELECT * FROM questions WHERE EventId = @eventId", new { eventId });
	string? eventText = await db.QueryFirstOrDefaultAsync<string>("SELECT Name FROM events WHERE Id = @eventId", new { eventId });
	string votingUrl = $"https://localhost:7134/vote/{eventId}/";
	QuestPDF.Settings.License = LicenseType.Community;

	var pdf = Document.Create(container =>
	{
		container.Page(page =>
		{
			page.Size(PageSizes.A4.Landscape());
			page.Margin(20);
			page.Content().Table(table =>
			{
				table.ColumnsDefinition(columns =>
				{
					columns.ConstantColumn(792 / 2 - 20);
					columns.ConstantColumn(792 / 2 - 20);
				});

				for (int i = 0; i < count * 2; i++)
				{
					var guid = Guid.NewGuid().ToString();
					var qrCodeData = new QRCodeGenerator().CreateQrCode(votingUrl + guid, QRCodeGenerator.ECCLevel.Q);
					var qrCode = new PngByteQRCode(qrCodeData).GetGraphic(3);

					table.Cell().Padding(10).Element(cell =>
					{
						cell.Column(column =>
						{
							column.Item().PaddingBottom(15).Text(eventText).FontSize(16).SemiBold().AlignCenter();
							foreach (var q in questions)
							{
								column.Item().Text(q.Text).FontSize(12).SemiBold();
								var choices = db.Query<Choice>("SELECT * FROM choices WHERE QuestionId = @QId", new { QId = q.Id });
								foreach (var choice in choices)
								{
									column.Item().Text($" - {choice.ChoiceText}").FontSize(10);
								}
							}
							column.Item().ExtendVertical().AlignBottom().PaddingBottom(20).Column(col =>
							{
								col.Item().Width(1, Unit.Inch).Image(qrCode).FitWidth();
								col.Item().Text($"GUID: {guid}").FontSize(8).AlignRight();
							});
						});
					});
				}
			});
		});
	});

	return Results.File(pdf.GeneratePdf(), "application/pdf");
});

app.MapPost("/vote/{eventId:int}/{guid:guid}", async (IDbConnection db, int eventId, Guid guid, List<Vote> submission) =>
{
	var existsGuid = await db.ExecuteScalarAsync<bool>(
		"SELECT COUNT(1) FROM vote_guids WHERE Guid = @Guid",
		new { Guid = guid }
	);
	if (existsGuid) return Results.BadRequest("Invalid or already used GUID");

	foreach (var vote in submission)
	{
		Vote newVote = new() { QuestionId = vote.QuestionId, ChoiceId = vote.ChoiceId, Guid = guid };
		await db.InsertAsync(newVote);
	}

	VoteGuids newGuid = new() { EventId = eventId, Guid = guid };
	await db.InsertAsync(newGuid);

	return Results.Ok("Vote submitted successfully");
});



app.Run();