using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.PublishBlog
{
    public class PublishBlogCommandHandler : IRequestHandler<PublishBlogCommand, Result<BlogDto>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public PublishBlogCommandHandler(
            IBlogRepository blogRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper)
        {
            _blogRepository = blogRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<BlogDto>> Handle(
            PublishBlogCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<BlogDto>.Failure(DomainErrors.Auth.InvalidToken);

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
                return Result<BlogDto>.Failure(DomainErrors.Blog.NotFound);

            if (blog.UserId != userId)
                return Result<BlogDto>.Failure(DomainErrors.Blog.NotOwner);

            if (blog.Status == "published")
                return Result<BlogDto>.Failure(DomainErrors.Blog.AlreadyPublished);

            if (blog.Status == "deleted")
                return Result<BlogDto>.Failure(DomainErrors.Blog.HasDeleted);

            if (string.IsNullOrEmpty(blog.Title))
                return Result<BlogDto>.Failure(DomainErrors.Blog.TitleRequired);

            if (string.IsNullOrEmpty(blog.ShortDescription))
                return Result<BlogDto>.Failure(DomainErrors.Blog.ShortDescriptionRequired);

            blog.Status = "published";
            blog.PublishedAt = DateTime.UtcNow;
            blog.UpdatedAt = DateTime.UtcNow;

            _blogRepository.Update(blog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<BlogDto>.Success(_mapper.Map<BlogDto>(blog));
        }
    }
}
