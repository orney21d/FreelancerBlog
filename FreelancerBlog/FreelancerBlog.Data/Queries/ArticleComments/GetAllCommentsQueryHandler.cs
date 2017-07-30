﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreelancerBlog.Core.Domain;
using FreelancerBlog.Core.Queries.Data.ArticleComments;
using FreelancerBlog.Data.EntityFramework;
using MediatR;

namespace FreelancerBlog.Data.Queries.ArticleComments
{
    public class GetAllCommentsQueryHandler : IRequestHandler<GetAllCommentsQuery, IQueryable<ArticleComment>>
    {
        private readonly FreelancerBlogContext _context;

        public GetAllCommentsQueryHandler(FreelancerBlogContext context)
        {
            _context = context;
        }

        public  IQueryable<ArticleComment> Handle(GetAllCommentsQuery message)
        {
            return  _context.ArticleComments.AsQueryable();
        }
    }
}