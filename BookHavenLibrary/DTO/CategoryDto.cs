﻿namespace BookHavenLibrary.DTO
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class CategoryUpdateDto : CategoryDto
    {
    }
}
