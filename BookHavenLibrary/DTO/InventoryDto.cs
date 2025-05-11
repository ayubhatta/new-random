namespace BookHavenLibrary.DTO
{
    public class InventoryDto
    {
        public int QuantityInStock { get; set; }
        public int ReorderThreshold { get; set; }
        public bool IsAvailable { get; set; }
    }

}
