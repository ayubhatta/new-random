namespace BookHavenLibrary.Dto
{
    public class CancelOrderDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
