﻿namespace BookHavenLibrary.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ShoppingCartId { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }
        public Book Book { get; set; } = null!;
        public ShoppingCart ShoppingCart { get; set; } = null!;
    }
}
