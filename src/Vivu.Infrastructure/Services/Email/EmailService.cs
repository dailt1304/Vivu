
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.DTOs.Responses.Payments;

namespace Vivu.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendVerificationEmailAsync(string email, string code)
    {
        var subject = "Verify Your Email - Vivu Travel Platform";
        var body = $@"
            <h2>Email Verification</h2>
            <p>Thank you for registering with Vivu Travel Platform!</p>
            <p>Your verification code is: <strong style='font-size: 24px;'>{code}</strong></p>
            <p>This code will expire in 10 minutes.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string code, string username)
    {
        var subject = "Reset Your Password - Vivu Travel Platform";
        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        </head>
        <body style='margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                    <td align='center' style='padding:20px 0;'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:8px;'>
                    
                            <tr>
                                <td style='padding:30px;text-align:center;background-color:#4F46E5;border-radius:8px 8px 0 0;'>
                                    <h1 style='color:#ffffff;margin:0;font-size:24px;'>Vivu Travel</h1>
                                </td>
                            </tr>
                    
                            <tr>
                                <td style='padding:40px 30px;'>
                                    <h2 style='color:#1f2937;margin:0 0 20px;font-size:20px;'>Reset Your Password</h2>
                                    <p style='color:#4b5563;font-size:16px;line-height:1.6;margin:0 0 20px;'>
                                        Hi {username},
                                    </p>
                                    <p style='color:#4b5563;font-size:16px;line-height:1.6;margin:0 0 20px;'>
                                        We received a request to reset your password. Use the code below to reset it:
                                    </p>
                                    <div style='background-color:#f3f4f6;padding:20px;border-radius:8px;text-align:center;margin:20px 0;'>
                                        <p style='color:#6b7280;font-size:14px;margin:0 0 10px;'>Your reset code:</p>
                                        <p style='font-size:36px;font-weight:bold;color:#4F46E5;margin:0;letter-spacing:8px;'>{code}</p>
                                    </div>
                                    <p style='color:#4b5563;font-size:16px;line-height:1.6;margin:20px 0;'>
                                        This code will expire in <strong>10 minutes</strong>.
                                    </p>
                                    <p style='color:#4b5563;font-size:16px;line-height:1.6;margin:20px 0;'>
                                        If you didn't request a password reset, please ignore this email or contact support if you have concerns.
                                    </p>
                                </td>
                            </tr>
                    
                            <tr>
                                <td style='padding:20px 30px;background-color:#fef3c7;border-left:4px solid #f59e0b;'>
                                    <p style='margin:0;font-size:14px;color:#92400e;'>
                                        <strong>🔒 Security Tip:</strong> Never share this code with anyone. Vivu will never ask for your password or reset code.
                                    </p>
                                </td>
                            </tr>
                    
                            <tr>
                                <td style='padding:30px;background-color:#f9fafb;border-radius:0 0 8px 8px;text-align:center;font-size:12px;color:#6b7280;'>
                                    <p style='margin:0 0 10px;'>© 2025 Vivu Travel Platform. All rights reserved.</p>
                                    <p style='margin:0;'>
                                        <a href='mailto:support@vivu.com' style='color:#4F46E5;text-decoration:none;'>Contact Support</a> |
                                        <a href='#' style='color:#6b7280;text-decoration:none;'>Privacy Policy</a>
                                    </p>
                                </td>
                            </tr>
                    
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendTransactionDetailsEmailAsync(string email, InvoiceDto invoice)
    {
        var subject = $"Transaction Details - Order #{invoice.Payment.OrderCode}";
        var body = GenerateTransactionDetailsEmailHtml(invoice);
        await SendEmailAsync(email, subject, body);
    }

    private string GenerateTransactionDetailsEmailHtml(InvoiceDto invoice)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                @media only screen and (max-width: 600px) {{
                    .email-container {{
                        width: 100% !important;
                        max-width: 600px !important;
                    }}
                    .content-padding {{
                        padding: 20px !important;
                    }}
                    .header {{
                        padding: 20px !important;
                    }}
                    .success-icon {{
                        width: 70px !important;
                        height: 70px !important;
                        line-height: 70px !important;
                    }}
                    .success-icon span {{
                        font-size: 40px !important;
                    }}
                    .title {{
                        font-size: 20px !important;
                    }}
                    .section-title {{
                        font-size: 15px !important;
                    }}
                    .package-name {{
                        font-size: 16px !important;
                    }}
                    .total-amount {{
                        font-size: 20px !important;
                    }}
                    .package-stats {{
                        display: block !important;
                        width: 100% !important;
                    }}
                    .package-stat-item {{
                        display: block !important;
                        width: 100% !important;
                        margin-bottom: 10px !important;
                    }}
                    .package-stat-spacer {{
                        display: none !important;
                    }}
                }}
            </style>
        </head>
        <body style='margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4;'>
                <tr>
                    <td align='center' style='padding:20px 10px;'>
                        <table class='email-container' width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:8px;max-width:600px;'>
                    
                            <!-- Header -->
                            <tr>
                                <td class='header' style='padding:30px;text-align:center;background-color:#4F46E5;border-radius:8px 8px 0 0;'>
                                    <h1 style='color:#ffffff;margin:0;font-size:24px;font-weight:bold;'>Vivu Travel</h1>
                                    <p style='color:#e0e7ff;margin:10px 0 0;font-size:14px;'>Transaction Confirmation</p>
                                </td>
                            </tr>
                    
                            <!-- Success Message -->
                            <tr>
                                <td class='content-padding' style='padding:30px;text-align:center;'>
                                    <table width='100%' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center'>
                                                <div class='success-icon' style='width:80px;height:80px;background-color:#10b981;border-radius:50%;display:inline-block;text-align:center;line-height:80px;margin:0 auto;'>
                                                    <span style='color:#ffffff;font-size:48px;font-weight:bold;'>✓</span>
                                                </div>
                                            </td>
                                        </tr>
                                    </table>
                                    <h2 class='title' style='color:#1f2937;margin:20px 0 10px;font-size:22px;font-weight:bold;'>Payment Received!</h2>
                                    <p style='color:#6b7280;font-size:14px;margin:0;line-height:1.5;'>Your subscription has been activated successfully.</p>
                                </td>
                            </tr>
                    
                            <!-- Transaction Details -->
                            <tr>
                                <td class='content-padding' style='padding:0 30px 30px;'>
                                    <div style='background-color:#f3f4f6;padding:20px;border-radius:8px;'>
                                        <h3 class='section-title' style='color:#1f2937;margin:0 0 15px;font-size:16px;font-weight:bold;'>Transaction Details</h3>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='font-size:14px;'>
                                            <tr>
                                                <td style='color:#6b7280;padding:8px 0;'>Order Code:</td>
                                                <td style='color:#1f2937;padding:8px 0;text-align:right;font-weight:600;word-break:break-all;'>{invoice.Payment.OrderCode}</td>
                                            </tr>
                                            <tr>
                                                <td style='color:#6b7280;padding:8px 0;white-space:nowrap;'>Transaction Date:</td>
                                                <td style='color:#1f2937;padding:8px 0;text-align:right;'>{invoice.Payment.TransactionDate:dd/MM/yyyy HH:mm:ss}</td>
                                            </tr>
                                            <tr>
                                                <td style='color:#6b7280;padding:8px 0;white-space:nowrap;'>Payment Method:</td>
                                                <td style='color:#1f2937;padding:8px 0;text-align:right;'>{invoice.Payment.PaymentMethod ?? "N/A"}</td>
                                            </tr>
                                            <tr>
                                                <td style='color:#6b7280;padding:8px 0;'>Status:</td>
                                                <td style='padding:8px 0;text-align:right;'>
                                                    <span style='background-color:{GetStatusBgColor(invoice.Status)};color:{GetStatusTextColor(invoice.Status)};padding:4px 12px;border-radius:12px;font-size:12px;font-weight:600;white-space:nowrap;'>{invoice.Status}</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </div>
                                </td>
                            </tr>
                    
                            <!-- Package Details -->
                            <tr>
                                <td class='content-padding' style='padding:0 30px 30px;'>
                                    <h3 class='section-title' style='color:#1f2937;margin:0 0 15px;font-size:16px;font-weight:bold;'>Subscription Package</h3>
                                    <div style='border:2px solid #e5e7eb;padding:20px;border-radius:8px;'>
                                        <p class='package-name' style='margin:0 0 5px;font-size:18px;font-weight:bold;color:#1f2937;'>{invoice.Package.Name}</p>
                                        {(!string.IsNullOrWhiteSpace(invoice.Package.Code) ? $"<p style='margin:0 0 10px;font-size:12px;color:#6b7280;'>Code: {invoice.Package.Code}</p>" : "")}
                                        {(!string.IsNullOrWhiteSpace(invoice.Package.Description) ? $"<p style='margin:10px 0;font-size:14px;color:#4b5563;line-height:1.5;'>{invoice.Package.Description}</p>" : "")}
                                        <table class='package-stats' width='100%' cellpadding='0' cellspacing='0' style='margin-top:15px;'>
                                            <tr>
                                                <td class='package-stat-item' style='width:48%;padding:10px;background-color:#eff6ff;border-radius:6px;vertical-align:top;'>
                                                    <p style='margin:0;font-size:12px;color:#6b7280;'>Duration</p>
                                                    <p style='margin:5px 0 0;font-size:18px;font-weight:bold;color:#2563eb;'>{invoice.Package.DurationDays} days</p>
                                                </td>
                                                <td class='package-stat-spacer' style='width:4%;'></td>
                                                <td class='package-stat-item' style='width:48%;padding:10px;background-color:#f0fdf4;border-radius:6px;vertical-align:top;'>
                                                    <p style='margin:0;font-size:12px;color:#6b7280;'>AI Requests/Day</p>
                                                    <p style='margin:5px 0 0;font-size:18px;font-weight:bold;color:#16a34a;'>{invoice.Package.MaxAiRequestPerDay}</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </div>
                                </td>
                            </tr>
                    
                            <!-- Payment Summary -->
                            <tr>
                                <td class='content-padding' style='padding:0 30px 30px;'>
                                    <table width='100%' cellpadding='0' cellspacing='0' style='font-size:15px;'>
                                        <tr style='border-top:2px solid #1f2937;'>
                                            <td style='color:#1f2937;padding:15px 0;font-size:18px;font-weight:bold;'>Total Amount Paid:</td>
                                            <td class='total-amount' style='color:#4F46E5;padding:15px 0;text-align:right;font-size:24px;font-weight:bold;white-space:nowrap;'>{invoice.Total:N0} VND</td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                    
                            {(!string.IsNullOrWhiteSpace(invoice.Notes) ? $@"
                            <tr>
                                <td class='content-padding' style='padding:0 30px 30px;'>
                                    <div style='background-color:#fef3c7;border-left:4px solid #f59e0b;padding:15px;border-radius:4px;'>
                                        <p style='margin:0;font-size:13px;color:#92400e;line-height:1.5;'><strong>Note:</strong> {invoice.Notes}</p>
                                    </div>
                                </td>
                            </tr>
                            " : "")}
                    
                            <!-- Help Section -->
                            <tr>
                                <td class='content-padding' style='padding:20px 30px;background-color:#eff6ff;border-top:1px solid #dbeafe;'>
                                    <p style='margin:0;font-size:14px;color:#1e40af;line-height:1.6;'>
                                        <strong>Need help?</strong> Contact our support team at 
                                        <a href='mailto:support@vivu.com' style='color:#2563eb;text-decoration:none;word-break:break-word;'>support@vivu.com</a>
                                    </p>
                                </td>
                            </tr>
                    
                            <!-- Footer -->
                            <tr>
                                <td class='content-padding' style='padding:30px;background-color:#f9fafb;border-radius:0 0 8px 8px;text-align:center;font-size:12px;color:#6b7280;'>
                                    <p style='margin:0 0 10px;line-height:1.5;'>© {DateTime.UtcNow.Year} Vivu Travel Platform. All rights reserved.</p>
                                    <p style='margin:0;line-height:1.8;'>
                                        <a href='mailto:support@vivu.com' style='color:#4F46E5;text-decoration:none;'>Contact Support</a>
                                        <span style='color:#d1d5db;margin:0 8px;'>|</span>
                                        <a href='#' style='color:#6b7280;text-decoration:none;'>Privacy Policy</a>
                                    </p>
                                </td>
                            </tr>
                    
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";
    }

    private string GetStatusBgColor(string status)
    {
        return status.ToLower() switch
        {
            "completed" => "#dcfce7",
            "success" => "#dcfce7",
            "pending" => "#fef3c7",
            "cancelled" => "#fee2e2",
            "failed" => "#fee2e2",
            _ => "#f3f4f6"
        };
    }

    private string GetStatusTextColor(string status)
    {
        return status.ToLower() switch
        {
            "completed" => "#166534",
            "success" => "#166534",
            "pending" => "#92400e",
            "cancelled" => "#991b1b",
            "failed" => "#991b1b",
            _ => "#1f2937"
        };
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPass"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);

        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
