﻿using FreelancerBlog.Core.DomainModels;
using MediatR;

namespace FreelancerBlog.Core.Commands.Data.ArticleTags
{
    public class CreateArticleTagsCommand : IRequest
    {
        public Article Article { get; set; }
        public string ArticleTags { get; set; }
    }
}