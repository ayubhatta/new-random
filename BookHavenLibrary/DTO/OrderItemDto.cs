namespace BookHavenLibrary.DTO
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public decimal PriceAtOrder { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }


        public class CreateOrderDto
        {
            public int UserId { get; set; }
            public List<CreateOrderItemDto> OrderItems { get; set; }
        }
        public class CreateOrderItemDto
        {
            public int BookId { get; set; }
            public int Quantity { get; set; }
        }
        public class UpdateOrderStatusDto
        {
            public string Status { get; set; }
        }

    }

}