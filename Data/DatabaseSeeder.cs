using Microsoft.EntityFrameworkCore;
using LibWiseApp.Models;

namespace LibWiseApp.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Books.CountAsync() >= 35) return;

        var users = await db.Users.ToListAsync();
        var adminId = users.First(u => u.Email == "admin@libwise.com").Id;
        var librarianId = users.First(u => u.Email == "librarian@libwise.com").Id;

        var categories = await db.Categories.ToListAsync();
        var fiction = categories.First(c => c.Name == "Fiction");
        var nonFiction = categories.First(c => c.Name == "Non-Fiction");
        var sciTech = categories.First(c => c.Name == "Science & Technology");
        var math = categories.First(c => c.Name == "Mathematics");
        var history = categories.First(c => c.Name == "History");
        var philosophy = categories.First(c => c.Name == "Philosophy");
        var reference = categories.First(c => c.Name == "Reference");

        var books = new List<Book>
        {
            new() { Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "9780061120084", CategoryId = fiction.Id, TotalCopies = 5, PublicationYear = 1960, ShelfLocation = "A1", Description = "A novel about racial injustice in the Deep South", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-60) },
            new() { Title = "1984", Author = "George Orwell", ISBN = "9780451524935", CategoryId = fiction.Id, TotalCopies = 4, PublicationYear = 1949, ShelfLocation = "A2", Description = "Dystopian social science fiction", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-58) },
            new() { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "9780743273565", CategoryId = fiction.Id, TotalCopies = 3, PublicationYear = 1925, ShelfLocation = "A3", Description = "Story of the mysteriously wealthy Jay Gatsby", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-55) },
            new() { Title = "Pride and Prejudice", Author = "Jane Austen", ISBN = "9780141439518", CategoryId = fiction.Id, TotalCopies = 6, PublicationYear = 1813, ShelfLocation = "A4", Description = "Romantic novel of manners", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-53) },
            new() { Title = "Atomic Habits", Author = "James Clear", ISBN = "9780735211292", CategoryId = nonFiction.Id, TotalCopies = 8, PublicationYear = 2018, ShelfLocation = "B1", Description = "An Easy & Proven Way to Build Good Habits", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-50) },
            new() { Title = "Sapiens", Author = "Yuval Noah Harari", ISBN = "9780062316097", CategoryId = nonFiction.Id, TotalCopies = 5, PublicationYear = 2011, ShelfLocation = "B2", Description = "A Brief History of Humankind", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-48) },
            new() { Title = "The Power of Habit", Author = "Charles Duhigg", ISBN = "9780812981605", CategoryId = nonFiction.Id, TotalCopies = 4, PublicationYear = 2012, ShelfLocation = "B3", Description = "Why We Do What We Do in Life and Business", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-45) },
            new() { Title = "Introduction to Algorithms", Author = "Thomas H. Cormen", ISBN = "9780262033848", CategoryId = sciTech.Id, TotalCopies = 3, PublicationYear = 2009, ShelfLocation = "C1", Description = "Comprehensive textbook on algorithms", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-42) },
            new() { Title = "The Pragmatic Programmer", Author = "Andrew Hunt", ISBN = "9780135957059", CategoryId = sciTech.Id, TotalCopies = 5, PublicationYear = 2019, ShelfLocation = "C2", Description = "Your Journey to Mastery", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-40) },
            new() { Title = "A Brief History of Time", Author = "Stephen Hawking", ISBN = "9780553380163", CategoryId = sciTech.Id, TotalCopies = 4, PublicationYear = 1988, ShelfLocation = "C3", Description = "From the Big Bang to Black Holes", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-38) },
            new() { Title = "The Selfish Gene", Author = "Richard Dawkins", ISBN = "9780198788607", CategoryId = sciTech.Id, TotalCopies = 3, PublicationYear = 1976, ShelfLocation = "C4", Description = "Gene-centered view of evolution", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-35) },
            new() { Title = "How to Prove It", Author = "Daniel J. Velleman", ISBN = "9781108439534", CategoryId = math.Id, TotalCopies = 3, PublicationYear = 2019, ShelfLocation = "D1", Description = "A Structured Approach to Proofs", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-33) },
            new() { Title = "Linear Algebra Done Right", Author = "Sheldon Axler", ISBN = "9783319110790", CategoryId = math.Id, TotalCopies = 4, PublicationYear = 2014, ShelfLocation = "D2", Description = "Undergraduate textbook on linear algebra", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new() { Title = "Calculus", Author = "James Stewart", ISBN = "9781285740621", CategoryId = math.Id, TotalCopies = 5, PublicationYear = 2015, ShelfLocation = "D3", Description = "Early Transcendentals", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-28) },
            new() { Title = "A People's History of the United States", Author = "Howard Zinn", ISBN = "9780062397348", CategoryId = history.Id, TotalCopies = 3, PublicationYear = 1980, ShelfLocation = "E1", Description = "American history from marginalized perspectives", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-25) },
            new() { Title = "Guns, Germs, and Steel", Author = "Jared Diamond", ISBN = "9780393354324", CategoryId = history.Id, TotalCopies = 4, PublicationYear = 1997, ShelfLocation = "E2", Description = "The Fates of Human Societies", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-23) },
            new() { Title = "The Histories", Author = "Herodotus", ISBN = "9780140449086", CategoryId = history.Id, TotalCopies = 2, PublicationYear = -440, ShelfLocation = "E3", Description = "Ancient historical account", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new() { Title = "Thus Spoke Zarathustra", Author = "Friedrich Nietzsche", ISBN = "9780140441182", CategoryId = philosophy.Id, TotalCopies = 3, PublicationYear = 1883, ShelfLocation = "F1", Description = "Philosophical novel", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-18) },
            new() { Title = "Meditations", Author = "Marcus Aurelius", ISBN = "9780140449334", CategoryId = philosophy.Id, TotalCopies = 5, PublicationYear = 180, ShelfLocation = "F2", Description = "Stoic philosophical writings", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new() { Title = "Encyclopedia of Library Science", Author = "John Smith", ISBN = "9781234567890", CategoryId = reference.Id, TotalCopies = 2, PublicationYear = 2020, ShelfLocation = "G1", Description = "Comprehensive reference work", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) }
        };
        db.Books.AddRange(books);

        var moreBooks = new List<Book>
        {
            new() { Title = "The Catcher in the Rye", Author = "J.D. Salinger", ISBN = "9780316769488", CategoryId = fiction.Id, TotalCopies = 4, PublicationYear = 1951, ShelfLocation = "A5", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new() { Title = "One Hundred Years of Solitude", Author = "Gabriel Garcia Marquez", ISBN = "9780060883287", CategoryId = fiction.Id, TotalCopies = 3, PublicationYear = 1967, ShelfLocation = "A6", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new() { Title = "Brave New World", Author = "Aldous Huxley", ISBN = "9780060850524", CategoryId = fiction.Id, TotalCopies = 5, PublicationYear = 1932, ShelfLocation = "A7", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new() { Title = "The Hobbit", Author = "J.R.R. Tolkien", ISBN = "9780547928227", CategoryId = fiction.Id, TotalCopies = 7, PublicationYear = 1937, ShelfLocation = "A8", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Title = "Dune", Author = "Frank Herbert", ISBN = "9780441172719", CategoryId = fiction.Id, TotalCopies = 4, PublicationYear = 1965, ShelfLocation = "A9", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new() { Title = "Outliers", Author = "Malcolm Gladwell", ISBN = "9780316017930", CategoryId = nonFiction.Id, TotalCopies = 6, PublicationYear = 2008, ShelfLocation = "B4", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Title = "Think and Grow Rich", Author = "Napoleon Hill", ISBN = "9781585424337", CategoryId = nonFiction.Id, TotalCopies = 5, PublicationYear = 1937, ShelfLocation = "B5", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Title = "Clean Code", Author = "Robert C. Martin", ISBN = "9780132350884", CategoryId = sciTech.Id, TotalCopies = 4, PublicationYear = 2008, ShelfLocation = "C5", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "Design Patterns", Author = "Erich Gamma", ISBN = "9780201633610", CategoryId = sciTech.Id, TotalCopies = 3, PublicationYear = 1994, ShelfLocation = "C6", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "The Art of War", Author = "Sun Tzu", ISBN = "9781590302255", CategoryId = philosophy.Id, TotalCopies = 6, PublicationYear = -500, ShelfLocation = "F3", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "The Republic", Author = "Plato", ISBN = "9780140455113", CategoryId = philosophy.Id, TotalCopies = 4, PublicationYear = -375, ShelfLocation = "F4", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "The Elements of Style", Author = "William Strunk Jr.", ISBN = "9780205309023", CategoryId = reference.Id, TotalCopies = 8, PublicationYear = 1918, ShelfLocation = "G2", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "A Brief History of the Philippines", Author = "Teodoro Agoncillo", ISBN = "9789715420796", CategoryId = history.Id, TotalCopies = 5, PublicationYear = 1960, ShelfLocation = "E4", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "Introduction to Statistics", Author = "Ronald Walpole", ISBN = "9780134468910", CategoryId = math.Id, TotalCopies = 4, PublicationYear = 2016, ShelfLocation = "D4", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Title = "The Philippine Constitution", Author = "Joaquin Bernas", ISBN = "9789712357983", CategoryId = reference.Id, TotalCopies = 3, PublicationYear = 2019, ShelfLocation = "G3", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        db.Books.AddRange(moreBooks);

        // Every copy starts on the shelf; PatchDataConsistencyAsync derives the real
        // AvailableCopies from whichever borrowing records get generated below.
        foreach (var book in books.Concat(moreBooks))
            book.AvailableCopies = book.TotalCopies;

        await db.SaveChangesAsync();

        var borrowers = new List<Borrower>
        {
            new() { Barcode = "BRW-001", FirstName = "Maria", LastName = "Santos", Email = "maria.santos@email.com", Phone = "09171234567", Grade = "Grade 11", IDNumber = "STU-2024-001", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-90) },
            new() { Barcode = "BRW-002", FirstName = "Juan", LastName = "Dela Cruz", Email = "juan.delacruz@email.com", Phone = "09172345678", Grade = "Grade 12", IDNumber = "STU-2024-002", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-85) },
            new() { Barcode = "BRW-003", FirstName = "Ana", LastName = "Gonzales", Email = "ana.gonzales@email.com", Phone = "09173456789", Grade = "Grade 10", IDNumber = "STU-2024-003", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-80) },
            new() { Barcode = "BRW-004", FirstName = "Pedro", LastName = "Reyes", Email = "pedro.reyes@email.com", Phone = "09174567890", Grade = "Grade 11", IDNumber = "STU-2024-004", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-75) },
            new() { Barcode = "BRW-005", FirstName = "Luisa", LastName = "Mendoza", Email = "luisa.mendoza@email.com", Phone = "09175678901", Grade = "Grade 9", IDNumber = "STU-2024-005", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-70) },
            new() { Barcode = "BRW-006", FirstName = "Carlos", LastName = "Flores", Email = "carlos.flores@email.com", Phone = "09176789012", Grade = "Grade 12", IDNumber = "STU-2024-006", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-65) },
            new() { Barcode = "BRW-007", FirstName = "Sofia", LastName = "Villanueva", Email = "sofia.villanueva@email.com", Phone = "09177890123", Grade = "Faculty", IDNumber = "FAC-001", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-60) },
            new() { Barcode = "BRW-008", FirstName = "Miguel", LastName = "Torres", Email = "miguel.torres@email.com", Phone = "09178901234", Grade = "Grade 10", IDNumber = "STU-2024-007", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-55) },
            new() { Barcode = "BRW-009", FirstName = "Elena", LastName = "Cruz", Email = "elena.cruz@email.com", Phone = "09179012345", Grade = "Faculty", IDNumber = "FAC-002", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-50) },
            new() { Barcode = "BRW-010", FirstName = "Ramon", LastName = "Aguilar", Email = "ramon.aguilar@email.com", Phone = "09170123456", Grade = "Grade 11", IDNumber = "STU-2024-008", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-45) }
        };
        db.Borrowers.AddRange(borrowers);

        var moreBorrowers = new List<Borrower>
        {
            new() { Barcode = "BRW-011", FirstName = "Isabella", LastName = "Ramos", Email = "isabella.ramos@email.com", Phone = "09171234511", Grade = "Grade 9", IDNumber = "STU-2024-009", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-40) },
            new() { Barcode = "BRW-012", FirstName = "Jose", LastName = "Garcia", Email = "jose.garcia@email.com", Phone = "09171234512", Grade = "Grade 10", IDNumber = "STU-2024-010", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-38) },
            new() { Barcode = "BRW-013", FirstName = "Cristina", LastName = "Lopez", Email = "cristina.lopez@email.com", Phone = "09171234513", Grade = "Grade 11", IDNumber = "STU-2024-011", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-36) },
            new() { Barcode = "BRW-014", FirstName = "Antonio", LastName = "Martinez", Email = "antonio.martinez@email.com", Phone = "09171234514", Grade = "Faculty", IDNumber = "FAC-003", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-34) },
            new() { Barcode = "BRW-015", FirstName = "Carmen", LastName = "Diaz", Email = "carmen.diaz@email.com", Phone = "09171234515", Grade = "Grade 12", IDNumber = "STU-2024-012", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-32) },
            new() { Barcode = "BRW-016", FirstName = "Andres", LastName = "Santos", Email = "andres.santos@email.com", Phone = "09171234516", Grade = "Grade 10", IDNumber = "STU-2024-013", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new() { Barcode = "BRW-017", FirstName = "Patricia", LastName = "Lim", Email = "patricia.lim@email.com", Phone = "09171234517", Grade = "Grade 9", IDNumber = "STU-2024-014", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-28) },
            new() { Barcode = "BRW-018", FirstName = "Fernando", LastName = "Co", Email = "fernando.co@email.com", Phone = "09171234518", Grade = "Faculty", IDNumber = "FAC-004", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-26) },
            new() { Barcode = "BRW-019", FirstName = "Angela", LastName = "Rivera", Email = "angela.rivera@email.com", Phone = "09171234519", Grade = "Grade 11", IDNumber = "STU-2024-015", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-24) },
            new() { Barcode = "BRW-020", FirstName = "Ricardo", LastName = "Domingo", Email = "ricardo.domingo@email.com", Phone = "09171234520", Grade = "Grade 12", IDNumber = "STU-2024-016", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-22) }
        };
        db.Borrowers.AddRange(moreBorrowers);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var rng = new Random(42);
        var records = new List<BorrowingRecord>();

        var allBooks = await db.Books.ToListAsync();
        var allBorrowers = await db.Borrowers.ToListAsync();
        var copiesCheckedOut = allBooks.ToDictionary(b => b.Id, _ => 0);

        for (int daysAgo = 40; daysAgo >= 0; daysAgo--)
        {
            int borrowedCount = daysAgo >= 8 ? rng.Next(2, 5) : (daysAgo switch
            {
                7 => 5, 6 => 4, 5 => 6, 4 => 4, 3 => 7, 2 => 5, 1 => 6, 0 => 4,
                _ => 3
            });

            for (int i = 0; i < borrowedCount; i++)
            {
                // A book can only be lent out if it actually has a free copy that day -
                // otherwise the seeded "available copies" would go negative.
                var available = allBooks.Where(b => copiesCheckedOut[b.Id] < b.TotalCopies).ToList();
                if (available.Count == 0) break;

                var book = available[rng.Next(available.Count)];
                var borrower = allBorrowers[rng.Next(allBorrowers.Count)];
                var borrowDate = now.Date.AddDays(-daysAgo).AddHours(rng.Next(8, 17)).AddMinutes(rng.Next(0, 60));
                bool isReturned = daysAgo >= 3 && rng.NextDouble() < 0.65;
                bool useTwoDayPeriod = daysAgo <= 5;

                var dueDate = useTwoDayPeriod ? borrowDate.AddDays(2) : borrowDate.AddDays(rng.Next(7, 21));
                var returnedAt = isReturned ? borrowDate.AddDays(useTwoDayPeriod ? 1 : rng.Next(1, 14)) : (DateTime?)null;

                var record = new BorrowingRecord
                {
                    BookId = book.Id,
                    BorrowerId = borrower.Id,
                    BorrowerBarcode = borrower.Barcode,
                    BorrowedByUserId = rng.NextDouble() < 0.5 ? adminId : librarianId,
                    BorrowedAt = borrowDate,
                    DueDate = dueDate,
                    ReturnedAt = returnedAt,
                    ReturnedByUserId = isReturned ? (rng.NextDouble() < 0.5 ? adminId : librarianId) : null,
                    Status = isReturned ? "Returned" : "Active",
                    WasExtended = useTwoDayPeriod && rng.NextDouble() < 0.2,
                    Remarks = isReturned ? (returnedAt > dueDate ? "Returned late" : "Returned on time") : null
                };
                records.Add(record);

                if (!isReturned)
                    copiesCheckedOut[book.Id]++;
            }
        }

        db.BorrowingRecords.AddRange(records);
        await db.SaveChangesAsync();

        await PatchDataConsistencyAsync(db);
    }

    // Idempotent, runs on every startup (not just first seed) so an already-deployed
    // database gets the same fixes as a freshly seeded one - see EnsureCreated patch
    // pattern used for schema columns in Program.cs.
    public static async Task PatchDataConsistencyAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        var activeCounts = await db.BorrowingRecords
            .Where(r => r.Status == "Active")
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);

        var books = await db.Books.ToListAsync();
        foreach (var book in books)
        {
            var correctAvailable = Math.Max(0, book.TotalCopies - activeCounts.GetValueOrDefault(book.Id));
            if (book.AvailableCopies != correctAvailable)
                book.AvailableCopies = correctAvailable;
        }

        var returned = await db.BorrowingRecords
            .Where(r => r.Status == "Returned" && r.ReturnedAt != null)
            .ToListAsync();
        foreach (var r in returned)
        {
            var correctRemark = r.ReturnedAt > r.DueDate ? "Returned late" : "Returned on time";
            if (r.Remarks != correctRemark)
                r.Remarks = correctRemark;
        }

        await db.SaveChangesAsync();

        var fineRule = await db.FineRules.FirstOrDefaultAsync(r => r.IsActive) ?? await db.FineRules.FirstOrDefaultAsync();
        if (fineRule == null) return;

        var unfined = await db.BorrowingRecords
            .Where(r => !r.Fines.Any() &&
                ((r.Status == "Active" && r.DueDate < now) ||
                 (r.Status == "Returned" && r.ReturnedAt != null && r.ReturnedAt > r.DueDate)))
            .ToListAsync();

        if (unfined.Count == 0) return;

        var staffIds = await db.Users.Select(u => u.Id).ToListAsync();
        if (staffIds.Count == 0) return;
        var rng = new Random();

        var newFines = new List<Fine>();
        foreach (var rec in unfined)
        {
            if (rec.Status == "Active")
            {
                var daysOverdue = (int)(now - rec.DueDate).TotalDays;
                if (daysOverdue < 1) continue;
                newFines.Add(new Fine
                {
                    BorrowingRecordId = rec.Id,
                    Amount = Math.Min(daysOverdue * fineRule.DailyFineRate, fineRule.MaxFine),
                    CalculatedAt = rec.DueDate.AddDays(1),
                    Status = "Unpaid",
                    Remarks = "Overdue fine"
                });
            }
            else
            {
                var daysOverdue = (int)(rec.ReturnedAt!.Value - rec.DueDate).TotalDays;
                if (daysOverdue < 1) continue;
                newFines.Add(new Fine
                {
                    BorrowingRecordId = rec.Id,
                    Amount = Math.Min(daysOverdue * fineRule.DailyFineRate, fineRule.MaxFine),
                    CalculatedAt = rec.DueDate.AddDays(1),
                    PaidAt = rec.ReturnedAt.Value.AddHours(rng.Next(1, 4)),
                    PaidByUserId = staffIds[rng.Next(staffIds.Count)],
                    Status = "Paid",
                    Remarks = "Paid fine"
                });
            }
        }

        if (newFines.Count > 0)
        {
            db.Fines.AddRange(newFines);
            await db.SaveChangesAsync();
        }
    }
}
