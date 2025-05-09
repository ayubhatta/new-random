namespace BookHavenLibrary.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int QuantityInStock { get; set; }
        public DateTime LastStockedDate { get; set; }
        public int ReorderThreshold { get; set; }
        public bool IsAvailable { get; set; }
        public virtual Book Book { get; set; }

    }
}
