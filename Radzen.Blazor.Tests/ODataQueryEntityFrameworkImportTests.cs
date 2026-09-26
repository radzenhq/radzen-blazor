using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ODataQueryEntityFrameworkImportTests
    {
        public class Owner
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Card
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public Owner Owner { get; set; }
            public List<Owner> Watchers { get; set; }
        }

        public class Board
        {
            public int Id { get; set; }
            public Owner Owner { get; set; }
            public Card[] Cards { get; set; }
            public IEnumerable<Card> Archive { get; set; }
            public ICollection<Card> Backlog { get; set; }
            public List<Card> Done { get; set; }
        }

        [Fact]
        public void ODataQuery_BindsIncludeThenIncludeAndWhereNextToEntityFrameworkAndLinq()
        {
            var query = new ODataQuery<Board>()
                .Where(board => board.Cards.Any(card => card.Title.Contains("x")) && board.Done.Count > 0)
                .Include(board => board.Owner)
                .Include(board => board.Cards).ThenInclude(card => card.Owner)
                .Include(board => board.Archive.Where(card => card.Id > 1).OrderBy(card => card.Title)).ThenInclude(card => card.Watchers)
                .Include(board => board.Backlog).ThenInclude(card => card.Watchers.OrderBy(owner => owner.Name).Take(1))
                .Include(board => board.Done.Take(3)).ThenInclude(card => card.Owner)
                .OrderBy(board => board.Owner.Name)
                .ThenBy(board => board.Id);

            var entityFramework = new List<Board>().AsQueryable()
                .Include(board => board.Cards).ThenInclude(card => card.Owner)
                .Where(board => board.Id > 0)
                .OrderBy(board => board.Id);

            Assert.Equal("$filter=Cards/any(card:contains(card/Title,'x')) and Done/$count gt 0&$expand=Owner,Cards($expand=Owner),Archive($filter=Id gt 1;$orderby=Title;$expand=Watchers),Backlog($expand=Watchers($orderby=Name;$top=1)),Done($top=3;$expand=Owner)&$orderby=Owner/Name,Id", query.ToString());
            Assert.Empty(entityFramework);
        }
    }
}
