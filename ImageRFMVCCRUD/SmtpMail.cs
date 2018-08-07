using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mail;

namespace ImageRFMVCCRUD
{
    public class SmtpMail
    {
        private const string DefaultEmailDomain = "@microsoft.com";
        private const string SmtpHost = "smtphost.redmond.corp.microsoft.com";

        /// <summary>
        /// Email -- SMTP using current process credentials.
        /// </summary>
        /// <param name="emailFrom">Email From.</param>
        /// <param name="emailTo">Email in to line.</param>
        /// <param name="emailCC">Email in cc line.</param>
        /// <param name="subject">Subject line.</param>
        /// <param name="msg">HTML Message.</param>
        /// <param name="priority"> is mail a hing priority</param>
        /// <returns>True if mail was sent</returns>
        public bool SendMail(
            string emailFrom,
            string emailTo,
            string emailCC,
            string subject,
            string msg,
            bool priority,
            string attachment)
        {
            bool sent = true;

            if (string.IsNullOrEmpty(emailFrom))
            {
                sent = false;
                throw new ArgumentException("You must specify an e-mail sender.", "emailFrom");
            }

            if (string.IsNullOrEmpty(emailTo))
            {
                sent = false;
                throw new ArgumentException("You must specify a recipient.", "emailTo");
            }

            try
            {
                System.Net.Mail.MailMessage mailMessage = new System.Net.Mail.MailMessage();

                mailMessage.IsBodyHtml = true;
                mailMessage.From = GetAddress(emailFrom);
                AddAddresses(emailTo, mailMessage.To);
                mailMessage.Subject = subject;
                mailMessage.Body = msg;
                mailMessage.Priority = System.Net.Mail.MailPriority.Normal;

                if (!String.IsNullOrEmpty(emailCC))
                {
                    AddAddresses(emailCC, mailMessage.CC);
                }

                if (!(string.IsNullOrEmpty(attachment)))
                {
                    mailMessage.Attachments.Add(new System.Net.Mail.Attachment(attachment));
                }

                if (priority)
                {
                    mailMessage.Priority = System.Net.Mail.MailPriority.High;
                }

                SmtpClient smtpClient = new SmtpClient(SmtpHost, 25);
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                smtpClient.Timeout = 60000;

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                sent = false;
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
            }

            return sent;
        }

        /// <summary>
        /// Adds the aliases to the <see cref="MainAddressCollection"/>
        /// </summary>
        /// <param name="aliases">Delimited list of aliases. Can be single alias</param>
        /// <param name="addresses">Address collection to add the aliases to</param>
        private void AddAddresses(string aliases, MailAddressCollection addresses)
        {
            char[] delimiterChars = { ' ', ',', ';' };

            if (!String.IsNullOrEmpty(aliases))
            {
                foreach (string alias in aliases.Split(delimiterChars))
                {
                    addresses.Add(GetAddress(alias));
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="MailAddress"/> instance for the alias
        /// </summary>
        /// <param name="alias">User alias or e-mail</param>
        /// <returns>MailAddress instance</returns>
        private MailAddress GetAddress(string alias)
        {
            if (!String.IsNullOrEmpty(alias))
            {
                string address = alias;
                if (alias.IndexOf('@') < 0)
                {
                    address += DefaultEmailDomain;
                }

                return new MailAddress(address);
            }

            return null;
        }
    }
}