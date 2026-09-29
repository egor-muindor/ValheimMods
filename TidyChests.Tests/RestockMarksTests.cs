using TidyChests.Restock;
using Xunit;

namespace TidyChests.Tests
{
    public class RestockMarksTests
    {
        [Fact]
        public void EmptyConfigMeansNothingMarked()
        {
            RestockMarks marks = RestockMarks.Parse(null);
            Assert.Empty(marks.Marks);
            Assert.Equal(0, marks.PercentAt(0, 0));
        }

        [Fact]
        public void ParsesSlotsPercentsAndItems()
        {
            RestockMarks marks = RestockMarks.Parse(" 0:1=100:$item_arrow_wood , 3:2=50, 4:0=75:$item_cookedmeat");

            Assert.Equal(3, marks.Marks.Count);
            Assert.Equal(100, marks.PercentAt(0, 1));
            Assert.Equal("$item_arrow_wood", marks.Find(0, 1)!.Item);
            Assert.Equal(50, marks.PercentAt(3, 2));
            Assert.Equal("", marks.Find(3, 2)!.Item);
            Assert.Equal(75, marks.PercentAt(4, 0));
            Assert.Equal(0, marks.PercentAt(1, 0));
        }

        [Fact]
        public void DropsMalformedEntriesRepeatsAndOutOfRangePercents()
        {
            RestockMarks marks = RestockMarks.Parse("1:1=50, 1:1=100, 2:2, x:1=50, 3:3=0, 4:4=101, 5:5=abc, -1:0=50, 6:6=100:");

            Assert.Equal(2, marks.Marks.Count);
            Assert.Equal(50, marks.PercentAt(1, 1));
            Assert.Equal(100, marks.PercentAt(6, 6));
            Assert.Equal("", marks.Find(6, 6)!.Item);
        }

        [Fact]
        public void CycleGoesFullThenHalfThenOff()
        {
            RestockMarks marks = RestockMarks.Parse("");

            Assert.Equal(100, marks.Cycle(2, 3, 50, "$item_arrow_wood"));
            Assert.Equal("$item_arrow_wood", marks.Find(2, 3)!.Item);
            Assert.Equal(50, marks.Cycle(2, 3, 50, null));
            Assert.Equal("$item_arrow_wood", marks.Find(2, 3)!.Item);
            Assert.Equal(0, marks.Cycle(2, 3, 50, null));
            Assert.Null(marks.Find(2, 3));
        }

        [Fact]
        public void CycleSkipsHalfWhenItIsAFullStack()
        {
            RestockMarks marks = RestockMarks.Parse("");

            Assert.Equal(100, marks.Cycle(0, 0, 100, null));
            Assert.Equal(0, marks.Cycle(0, 0, 100, null));
        }

        [Fact]
        public void HandEditedPercentGoesOffOnTheNextPress()
        {
            RestockMarks marks = RestockMarks.Parse("0:0=30");
            Assert.Equal(0, marks.Cycle(0, 0, 50, null));
        }

        [Fact]
        public void RememberUpdatesOnlyMarkedSlotsAndReportsChanges()
        {
            RestockMarks marks = RestockMarks.Parse("0:1=100");

            Assert.True(marks.Remember(0, 1, "$item_cookedmeat"));
            Assert.False(marks.Remember(0, 1, "$item_cookedmeat"));
            Assert.False(marks.Remember(0, 1, ""));
            Assert.False(marks.Remember(5, 5, "$item_wood"));
            Assert.Null(marks.Find(5, 5));
            Assert.Equal("$item_cookedmeat", marks.Find(0, 1)!.Item);
        }

        [Fact]
        public void FormatsBackIntoTheConfigLine()
        {
            RestockMarks marks = RestockMarks.Parse("0:1=100:$item_arrow_wood, 3:2=50");
            marks.Cycle(5, 0, 50, "$item_cookedmeat");

            Assert.Equal("0:1=100:$item_arrow_wood, 3:2=50, 5:0=100:$item_cookedmeat", marks.Format());
            Assert.Equal(marks.Format(), RestockMarks.Parse(marks.Format()).Format());
        }
    }
}
