using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Repositories.v2;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Repositories.v2;

public class NotesRepository(FoodDiaryContext context) : INotesRepository
{
    public async Task<IReadOnlyCollection<Note>> FindByDate(DateOnly date, CancellationToken cancellationToken)
    {
        return await context.Notes
            .AsNoTracking()
            .Where(n => n.Date == date)
            .Include(n => n.Product)
            .OrderBy(n => n.Time)
            .ThenBy(n => n.DisplayOrder)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Note>> FindByDateRange(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        return await context.Notes
            .AsNoTracking()
            .Where(n => n.Date >= from && n.Date <= to)
            .Include(n => n.Product)
            .OrderByDescending(n => n.Date)
            .ThenBy(n => n.Time)
            .ThenBy(n => n.DisplayOrder)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Note?> FindById(int id, CancellationToken cancellationToken)
    {
        return await context.Notes.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyCollection<Note>> FindByIds(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        return await context.Notes
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(cancellationToken);
    }

    public Task Add(Note note, CancellationToken cancellationToken)
    {
        context.Notes.Add(note);
        return context.SaveChangesAsync(cancellationToken);
    }

    public Task AddRange(IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        context.Notes.AddRange(notes);
        return context.SaveChangesAsync(cancellationToken);
    }

    public Task Update(Note note, CancellationToken cancellationToken)
    {
        context.Update(note);
        return context.SaveChangesAsync(cancellationToken);
    }
}
