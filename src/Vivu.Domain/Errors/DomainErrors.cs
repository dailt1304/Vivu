using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Errors
{
    public static class DomainErrors
    {
        public static class User
        {
            public static readonly Error NotFound = new(
                "User.NotFound",
                "User was not found.");

            public static Error NotFoundById(Guid id) => new(
                "User.NotFound",
                $"User with ID '{id}' was not found.");

            public static Error NotFoundByEmail(string email) => new(
                "User.NotFound",
                $"User with email '{email}' was not found.");

            public static readonly Error EmailAlreadyExists = new(
                "User.EmailAlreadyExists",
                "Email has already been registered.");

            public static readonly Error WrongPassword = new(
                "User.WrongPassword",
                "Email or password is incorrect.");

            public static readonly Error Banned = new(
                "User.Banned",
                "User account has been banned.");

            public static readonly Error AlreadyBanned = new(
                "User.AlreadyBanned",
                "User is already banned.");

            public static readonly Error NotBanned = new(
                "User.NotBanned",
                "User is not currently banned.");

            public static readonly Error EmailNotVerified = new(
                "User.EmailNotVerified",
                "Email has not been verified.");

            public static readonly Error InvalidRefreshToken = new(
                "User.InvalidRefreshToken",
                "Refresh token is invalid or expired.");
        }

        public static class Role
        {
            public static readonly Error NotFound = new(
                "Roles.NotFound",
                "Role was not found.");

            public static Error NotFoundByRole(string roleName) => new(
                "Role.NotFound",
                $"Role name '{roleName}' was not found.");
        }

        public static class Trip
        {
            public static readonly Error NotFound = new(
                "Trip.NotFound",
                "Trip was not found.");

            public static readonly Error AlreadyMember = new(
                "Trip.AlreadyMember",
                "You are already a member of this trip.");

            public static Error NotFoundById(Guid id) => new(
                "Trip.NotFound",
                $"Trip with ID '{id}' was not found.");

            public static Error TripDeleted(Guid id) => new(
                "Trip.IsDelete",
                $"Trip with ID '{id}' was delete.");

            public static Error AlreadyDeleted(Guid id) => new(
                "Trip.AlreadyDeleted",
                $"Trip with ID '{id}' has already been deleted.");

            public static Error NotFoundByTripDay(Guid tripDayId) => new(
                "Trip.NotFound",
                $"Trip day with ID '{tripDayId}' was not found.");

            public static readonly Error DateInvalid = new(
                "Trip.DateInvalid",
                "Start date must be earlier than end date.");

            public static readonly Error AlreadyStarted = new(
                "Trip.AlreadyStarted",
                "Cannot modify trip that has already started.");

            public static readonly Error AccessDenied = new(
                "Trip.AccessDenied",
                "You don't have permission to access this trip.");

            public static readonly Error MemberLimitReached = new(
                "Trip.MemberLimitReached",
                "Trip has reached maximum number of members.");

            public static readonly Error InvalidInviteCode = new(
                "Trip.InvalidInviteCode",
                "Invalid or expired invite code.");

            public static readonly Error TripLimitReached = new(
                "Trip.TripLimitReached",
                "You have reached the maximum number of trips for free users. Upgrade to premium to create more trips.");

            public static Error TripIncomplete(string message) => new(
                "Trip.Incomplete",
                message);

            public static readonly Error DatabaseError = new(
                "Trip.DatabaseError",
                "Failed to save trip to database. Please try again.");

            public static readonly Error UnexpectedError = new(
                "Trip.UnexpectedError",
                "An unexpected error occurred while creating your trip. Please try again.");

            public static readonly Error NotCompleted = new(
                "Trip.NotCompleted",
                "Trip must be completed before rating.");

            public static readonly Error NotOwner = new(
                "Trip.NotOwner",
                "Only trip owner can rate the trip.");

            public static readonly Error AlreadyRated = new(
                "Trip.AlreadyRated",
                "Trip has already been rated.");

            public static readonly Error InvalidSearchTerm = new(
                "Trip.InvalidSearchTerm",
                "Search term is invalid or empty.");

            public static readonly Error NotPublic = new(
                "Trip.NotPublic",
                "Only public trips can be favorited.");

            public static readonly Error AlreadyFavorited = new(
                "Trip.AlreadyFavorited",
                "You have already favorited this trip.");

            public static readonly Error NotFavorited = new(
                "Trip.NotFavorited",
                "You have not favorited this trip.");

            public static readonly Error TripIsPrivate = new(
                "Trip.TripIsPrivate",
                "This trip is private and cannot be copied.");

            public static readonly Error CreationFailed = new(
                "Trip.CreationFailed",
                "Failed to create trip. Please try again.");
        }

        public static class Location
        {
            public static readonly Error NotFound = new(
                "Location.NotFound",
                "Location was not found.");

            public static Error NotFoundById(Guid id) => new(
                "Location.NotFound",
                $"Location with ID '{id}' was not found.");

            public static readonly Error NotVerified = new(
                "Location.NotVerified",
                "Location has not been verified yet.");

            public static readonly Error AlreadyReported = new(
                "Location.AlreadyReported",
                "You have already reported this location.");

            public static readonly Error DuplicatePendingSubmission = new(
                "Location.DuplicatePendingSubmission",
                "You already have a pending submission for this location.");

            public static readonly Error InvalidSearchTerm = new(
                "Location.InvalidSearchTerm",
                "Search term is invalid or empty.");

            public static readonly Error DuplicatedNameAndAddress = new Error(
                "Location.DuplicatedNameAndAddress",
                "A location with the same name and address already exists.");

            public static readonly Error DuplicatedNameAndCoordinates = new Error(
                "Location.DuplicatedNameAndCoordinates",
                "A location with the same name and coordinates already exists.");

            public static readonly Error UsedInActiveTrips = new(
                "Location.UsedInActiveTrips",
                "Cannot delete location because it is being used in active trips.");

            public static readonly Error AlreadyDeleted = new(
                "Location.AlreadyDeleted",
                "Location has already been deleted.");

            public static readonly Error NoPendingReport = new(
                "Location.NoPendingReport",
                "No pending report found for this location.");

            public static Error InvalidLocations(List<Guid> locationguids) => new(
                "Trip.InvalidLocations",
                $"The following location IDs do not exist: {string.Join(", ", locationguids)}");

            public static readonly Error ImageUploadFailed = new(
                "Location.ImageUploadFailed",
                "Failed to upload location image. Please try again.");

            public static readonly Error SaveFailed = new(
                "Location.SaveFailed",
                "Failed to save location. Please try again.");

            public static readonly Error CannotUpdateApprovedLocation = new(
                "Location.CannotUpdateApprovedLocation",
                "Cannot update location that has already been approved. Only pending locations can be updated.");

            public static readonly Error UnauthorizedToUpdate = new(
                "Location.UnauthorizedToUpdate",
                "You are not authorized to update this location. Only the creator can update their submitted location.");
        }

        public static class Collection
        {
            public static readonly Error NotFound = new(
                "Collection.NotFound",
                "Collection was not found.");

            public static readonly Error AccessDenied = new(
                "Collection.AccessDenied",
                "You don't have permission to access this collection.");

            public static readonly Error NameAlreadyExists = new(
                "Collection.NameAlreadyExists",
                "A collection with this name already exists.");

            public static readonly Error LocationAlreadyInCollection = new(
                "Collection.LocationAlreadyInCollection",
                "This location is already in the collection.");

            public static readonly Error LocationNotInCollection = new(
                "Collection.LocationNotInCollection",
                "This location is not in the collection.");
        }

        public static class LocationCategory
        {
            public static readonly Error NotFound = new(
                "LocationCategory.NotFound",
                "Location category was not found.");

            public static Error NotFoundById(Guid id) => new(
                "LocationCategory.NotFound",
                $"Location category with ID '{id}' was not found.");

            public static readonly Error HasLocations = new(
                "LocationCategory.HasLocations",
                "Không thể xoá danh mục này vì đang có địa điểm sử dụng.");

            public static readonly Error DuplicateName = new(
                "LocationCategory.DuplicateName",
                "Tên danh mục đã tồn tại trong hệ thống.");
        }

        public static class LocationReport
        {
            public static readonly Error RateLimitExceeded = new(
                "LocationReport.RateLimitExceeded",
                "You have exceeded the maximum number of reports allowed per day (3 reports).");

            public static readonly Error NotFound = new(
                "LocationReport.NotFound",
                "Location report was not found.");

            public static readonly Error NotReportOwner = new(
                "LocationReport.NotReportOwner",
                "Bạn không có quyền chỉnh sửa báo cáo này.");

            public static readonly Error CannotUpdateNonPending = new(
                "LocationReport.CannotUpdateNonPending",
                "Chỉ có thể chỉnh sửa báo cáo khi đang ở trạng thái chờ duyệt.");

            public static Error NotFoundById(Guid id) => new(
                "LocationReport.NotFound",
                $"Report with ID '{id}' was not found");

            public static readonly Error AlreadyProcessed = new(
                "LocationReport.AlreadyProcessed",
                "This report has already been processed");

            public static readonly Error UpdateFailed = new(
                "LocationReport.UpdateFailed",
                "Failed to update location report");

            public static readonly Error ReviewError = new(
                "LocationReport.ReviewError",
                "An error occurred while reviewing the report");

            public static readonly Error SaveFailed = new(
                "LocationReport.SaveFailed",
                "Failed to save location report");
        }

        public static class Blog
        {
            public static readonly Error NotFound = new(
                "Blog.NotFound",
                "Blog post was not found.");

            public static readonly Error AccessDenied = new(
                "Blog.AccessDenied",
                "You don't have permission to modify this blog.");

            public static readonly Error InvalidStatus = new(
                "Blog.InvalidStatus",
                "Cannot publish draft blog without content.");

            public static readonly Error CannotUpdate = new(
                "Blog.CannotUpdate",
                "Blog cannot be updated in its current status.");

            public static readonly Error NotOwner = new(
                "Blog.NotOwner",
                "You are not owner of this blog");

            public static readonly Error HasDeleted = new(
                "Blog.HasDeleted",
                "This blog has been delete");

            public static readonly Error AlreadyPublished = new(
                "Blog.AlreadyPublished",
                "Blog has already been published.");

            public static readonly Error TitleRequired = new(
                "Blog.TitleRequired",
                "Blog title is required before publishing.");

            public static readonly Error ShortDescriptionRequired = new(
                "Blog.ShortDescriptionRequired",
                "Short description is required before publishing.");

            public static readonly Error CoverImageUploadFailed = new(
                "Blog.CoverImageUploadFailed",
                "Failed to upload cover image.");

            public static readonly Error NotPublished = new(
                "Blog.NotPublished",
                "This blog post is not published and cannot be viewed.");

            public static readonly Error AlreadyBanned = new(
                "Blog.AlreadyBanned",
                "This blog post has already been banned.");

            public static readonly Error AlreadyLiked = new(
                "Blog.AlreadyLiked",
                "You have already liked this blog post.");

            public static readonly Error NotLiked = new(
                "Blog.NotLiked",
                "You have not liked this blog post.");

            public static readonly Error AlreadyBookmarked = new(
                "Blog.AlreadyBookmarked",
                "You have already bookmarked this blog post.");

            public static readonly Error NotBookmarked = new(
                "Blog.NotBookmarked",
                "You have not bookmarked this blog post.");

            public static readonly Error InvalidSearchTerm = new(
                "Blog.InvalidSearchTerm",
                "Search term is invalid or empty.");
        }

        public static class BlogReport
        {
            public static readonly Error RateLimitExceeded = new(
                "BlogReport.RateLimitExceeded",
                "You have exceeded the maximum number of reports allowed per day (3 reports).");

            public static readonly Error NotFound = new(
                "BlogReport.NotFound",
                "Blog report was not found.");

            public static Error NotFoundById(Guid id) => new(
                "BlogReport.NotFound",
                $"Blog report with ID '{id}' was not found.");

            public static readonly Error AlreadyProcessed = new(
                "BlogReport.AlreadyProcessed",
                "This report has already been processed.");

            public static readonly Error AlreadyReported = new(
                "BlogReport.AlreadyReported",
                "You have already reported this blog.");

            public static readonly Error SaveFailed = new(
                "BlogReport.SaveFailed",
                "Failed to save blog report.");

            public static readonly Error UpdateFailed = new(
                "BlogReport.UpdateFailed",
                "Failed to update blog report.");

            public static readonly Error ReviewError = new(
                "BlogReport.ReviewError",
                "An error occurred while reviewing the blog report.");
        }

        public static class BlogComment
        {
            public static readonly Error NotFound = new(
                "BlogComment.NotFound",
                "Comment was not found.");

            public static readonly Error AccessDenied = new(
                "BlogComment.AccessDenied",
                "You don't have permission to delete this comment.");
        }

        public static class Auth
        {
            public static readonly Error InvalidCredentials = new(
                "Auth.InvalidCredentials",
                "Invalid email or password.");

            public static readonly Error InvalidGoogleToken = new(
               "Auth.InvalidGoogleToken",
               "Invalid Google ID token");

            public static readonly Error EmailNotVerified = new(
                "Auth.EmailNotVerified",
                "Email was note verify by Google");

            public static readonly Error InvalidToken = new(
                "Auth.InvalidToken",
                "Token is invalid or expired.");

            public static readonly Error Forbidden = new(
                "Auth.Forbidden",
                "You do not have permission to perform this action.");

            public static readonly Error OtpExpired = new(
                "Auth.OtpExpired",
                "OTP code has expired.");

            public static readonly Error OtpInvalid = new(
                "Auth.OtpInvalid",
                "Invalid OTP code.");

            public static readonly Error OtpAlreadyUsed = new(
                "Auth.OtpAlreadyUsed",
                "OTP code has already been used.");

            public static readonly Error EmailAlreadyVerified = new(
                "Auth.EmailAlreadyVerified",
                "Email has already been verified.");

            public static readonly Error TooManyOtpRequests = new(
                "Auth.TooManyOtpRequests",
                "Too many OTP requests. Please try again later.");

            public static readonly Error TooManyOtpVerifyAttempts = new(
                "Auth.TooManyOtpVerifyAttempts",
                "Too many failed attempts. Please request a new OTP.");

            public static readonly Error InvalidRefreshToken = new(
                "Auth.InvalidRefreshToken",
                "Invalid Refresh token");

            public static readonly Error RefreshTokenExpired = new(
                "Auth.RefreshTokenExpired",
                "Refresh token has expired. Please login again");

            public static readonly Error RefreshTokenRevoked = new(
                "Auth.RefreshTokenRevoked",
                "Refresh token has been revoked");

            public static readonly Error TokenReuseDetected = new(
                "Auth.TokenReuseDetected",
                "Unusual token usage detected. All login sessions have been logged out for security reasons.");
        }

        public static class Subscription
        {
            public static readonly Error PackageNotFound = new(
                "Subscription.PackageNotFound",
                "Subscription package was not found.");

            public static readonly Error AlreadySubscribed = new(
                "Subscription.AlreadySubscribed",
                "User already has an active subscription.");

            public static readonly Error LimitReached = new(
                "Subscription.LimitReached",
                "AI request limit has been reached for today.");

            public static Error CodeExists(string code) => new(
                "Subscription.CodeExists",
                $"Subscription package with code '{code}' already exists.");

            public static readonly Error CreateFailed = new(
                "Subscription.CreateFailed",
                "Failed to create subscription package.");

            public static readonly Error UpdateFailed = new(
                "Subscription.UpdateFailed",
                "Failed to update subscription package.");

            public static readonly Error DeleteFailed = new(
                "Subscription.DeleteFailed",
                "Failed to delete subscription package.");

            public static readonly Error SubscriptionExpired = new(
                "Subscription.Expired",
                "Your subscription has expired. Please purchase a new plan.");
        }

        public static class AIErrors
        {
            public static Error RateLimitExceeded(int remaining, DateTime resetAt) => new(
                    "AI.RateLimitExceeded",
                    $"Daily AI request limit exceeded. Remaining: {remaining}. Resets at: {resetAt:yyyy-MM-dd HH:mm:ss}");

            public static Error InvalidResponse(string reason) => new(
                    "AI.InvalidResponse",
                    $"AI returned invalid response: {reason}");

            public static Error ServiceUnavailable(string provider) => new(
                    "AI.ServiceUnavailable",
                    $"AI service '{provider}' is temporarily unavailable");

            public static Error ParseError(string details) => new(
                    "AI.ParseError",
                    $"Failed to parse AI response: {details}");

            public static Error Timeout(int seconds) => new(
                     "AI.Timeout",
                    $"AI request timed out after {seconds} seconds");

            public static readonly Error Unexpected = new(
                    "AI.UnexpectedError",
                    "An unexpected error occurred while generating your trip plan. Please try again.");
        }

        public static class TripLocation
        {
            public static readonly Error TimeConflict = new(
                "TripLocation.TimeConflict",
                "This location is already scheduled during the selected time..");

            public static readonly Error NotFound = new(
                "TripLocation.NotFound",
                "Trip location was not found.");

            public static Error NotFoundById(Guid id) => new(
                "TripLocation.NotFound",
                $"Trip location with ID '{id}' was not found.");

            public static Error InvalidIds = new(
                "TripLocation.InvalidIds",
                "One or more trip location IDs are invalid.");

            public static Error InvalidCount = new(
                "TripLocation.InvalidCount",
                "All trip locations must be included in the reorder operation.");

            public static readonly Error MaxAlternativesReached = new(
                "TripLocation.MaxAlternativesReached",
                "Một địa điểm chỉ được phép có tối đa 2 phương án dự phòng.");

            public static readonly Error AlternativeNotFound = new(
                "TripLocation.AlternativeNotFound",
                "Phương án dự phòng không tồn tại.");

            public static readonly Error AlternativeAlreadyExists = new(
                "TripLocation.AlternativeAlreadyExists",
                "Địa điểm này đã được thêm làm dự phòng rồi.");
        }

        public static class TripDay
        {
            public static readonly Error NotFound = new(
                "TripDay.NotFound",
                "Trip day was not found.");

            public static Error NotFoundById(Guid id) => new(
                "TripDay.NotFound",
                $"Trip day with ID '{id}' was not found.");

            public static readonly Error InvalidDayIndex = new(
                "TripDay.InvalidDayIndex",
                "Day index must be greater than 0.");

            public static readonly Error DayIndexConflict = new(
                "TripDay.DayIndexConflict",
                "A day with this index already exists in the trip.");

            public static readonly Error DateOutOfRange = new(
                "TripDay.DateOutOfRange",
                "Day date must be between trip start date and end date.");
        }

        public static class TripMember
        {
            public static readonly Error NotFound = new(
                "TripMember.NotFound",
                "Trip member was not found.");

            public static readonly Error AlreadyExists = new(
                "TripMember.AlreadyExists",
                "User is already a member of this trip.");

            public static readonly Error NotOwner = new(
                "TripMember.NotOwner",
                "You are not owner.");

            public static readonly Error NotFoundMember = new(
                "TripMember.NotFoundMember",
                "Cannot found this member in this trip.");

            public static readonly Error CannotRemoveSelf = new(
                "TripMember.CannotRemoveSelf",
                "Cannot remove yourself from the trip. Leave the trip instead.");

            public static readonly Error InvalidRoleChange = new(
                "TripMember.InvalidRoleChange",
                "Cannot change member role to Owner.");

            public static readonly Error OwnerCannotLeave = new(
                "TripMember.OwnerCannotLeave",
                "Trip owner cannot leave the trip. Delete the trip instead.");

            public static readonly Error NotMember = new(
                "TripMember.NotMember",
                "You are not member in this trip.");
        }

        public static class Cities
        {
            public static readonly Error NotFound = new(
                "City.NotFound",
                "City was not found.");

            public static Error NotFoundById(Guid id) => new(
                "City.NotFound",
                $"City with ID '{id}' was not found.");

            public static Error AlreadyExistByName(string name) => new(
                "City.AlreadyExist",
                $"City with Name '{name}' was already exist in database"
            );
        }

        public static class Countries
        {
            public static readonly Error NotFound = new(
                "Country.NotFound",
                "Country was not found.");

            public static Error NotFoundById(Guid id) => new(
                "Country.NotFound",
                $"Country with ID '{id}' was not found.");
        }

        public static class Chat
        {
            public static readonly Error ReplyMessageNotFound = new(
                "Chat.ReplyMessageNotFound",
                "Reply target message not found.");

            public static readonly Error MessageNotFound = new(
                "Chat.MessageNotFound",
                "Chat message was not found.");

            public static readonly Error NotMessageOwner = new(
                "Chat.NotMessageOwner",
                "You do not have permission to delete this message.");
        }

        public static class Destinations
        {
            public static readonly Error Error = new(
                "Destinations.Error",
                "Failed to retrieve trending destinations");
        }

        public static class ApplyModification
        {
            public static readonly Error ConcurrencyError = new(
                "ApplyModification.ConcurrencyError",
                "Đã có lỗi xảy ra khi lưu thay đổi sau nhiều lần thử. Vui lòng thử lại sau.");

            public static Error DayNotFound(int dayIndex) => new(
                "ApplyModification.DayNotFound",
                $"Không tìm thấy Ngày {dayIndex}.");

            public static Error DayNotFoundInTrip(int dayIndex) => new(
                "ApplyModification.DayNotFound",
                $"Không tìm thấy Ngày {dayIndex} trong chuyến đi.");

            public static Error DayNotFoundForRemove(int dayIndex) => new(
                "ApplyModification.DayNotFound",
                $"Không tìm thấy Ngày {dayIndex} cần xóa.");

            public static readonly Error LocationNotFoundForRemove = new(
                "ApplyModification.LocationNotFound",
                "Khoảng thời gian chứa địa điểm cần xóa không tồn tại.");

            public static readonly Error LocationNotFoundForUpdateTime = new(
                "ApplyModification.LocationNotFound",
                "Địa điểm cần cập nhật giờ không tồn tại.");

            public static readonly Error OldLocationNotFoundForUpdate = new(
                "ApplyModification.LocationNotFound",
                "Địa điểm cũ cần thay thế không tồn tại.");
        }
        public static class Payment
        {
            public static readonly Error TransactionNotFound = new(
                "Payment.TransactionNotFound",
                "Transaction was not found.");

            public static readonly Error AccessDenied = new(
                "Payment.AccessDenied",
                "You do not have permission to access this transaction.");

            public static readonly Error CreateFailed = new(
                "Payment.CreateFailed",
                "Failed to create payment link. Please try again.");

            public static readonly Error CancelFailed = new(
                "Payment.CancelFailed",
                "Failed to cancel payment. Please try again.");

            public static readonly Error CannotCancelNonPendingTransaction = new(
                "Payment.CannotCancelNonPendingTransaction",
                "Only pending transactions can be cancelled.");

            public static readonly Error InvalidWebhookSignature = new(
                "Payment.InvalidWebhookSignature",
                "Webhook signature verification failed.");

            public static readonly Error WebhookProcessingFailed = new(
                "Payment.WebhookProcessingFailed",
                "Failed to process payment webhook.");

            public static readonly Error InvoiceNotFound = new(
                "Payment.InvoiceNotFound",
                "Invoice was not found.");

            public static readonly Error InvoiceGenerationFailed = new(
                "Payment.InvoiceGenerationFailed",
                "Failed to generate invoice. Please try again.");

            public static readonly Error EmailSendFailed = new(
                "Payment.EmailSendFailed",
                "Failed to send email. Please try again.");
        }

        public static class Notification
        {
            public static readonly Error NotFound = new(
                "Notification.NotFound",
                "Notification was not found.");

            public static Error NotFoundById(Guid id) => new(
                "Notification.NotFound",
                $"Notification with ID '{id}' was not found.");

            public static readonly Error CreationFailed = new(
                "Notification.CreationFailed",
                "Failed to create notification. Please try again.");

            public static readonly Error AccessDenied = new(
                "Notification.AccessDenied",
                "You don't have permission to access this notification.");
        }
    }
}