﻿namespace CourseLibrary.API.ResourseParameters
{
    public class AuthorResourseParameters
    {
        const int maxPageSize = 20;
        public string? MainCategory { get; set; }
        public string? SearchQurey { get; set; }
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
    }
}