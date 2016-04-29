using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using PanoptoHierarchy.PanoptoAccessManagement;
using PanoptoHierarchy.PanoptoSessionManagement;

namespace PanoptoHierarchy
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args == null)
            {
                writeLine("The program didn't check for arguments"); // Check for null array
                Environment.Exit(-1);
            }
            else if (args.Length != 4)
            {
                writeLine(
                    "Please run this program with the arguments /u AdminUserName /p Password");
                Environment.Exit(-2);
            }
            else if (!args[0].Equals("/u") || !args[2].Equals("/p"))
            {
                writeLine(
                    "Please run this program with the arguments /u AdminUserName /p Password " +
                    "   The order is important");
                Environment.Exit(-3);
            }
            else
            {
                if (AreThereCourseFoldersInTheRoot(args[1], args[3]))
                {
                    int numberOfFolders = RunCourseHierarchy(args[1], args[3]);
                    writeLine(numberOfFolders + " courses processed");
                    Environment.Exit(numberOfFolders);
                }
                else
                {
                    writeLine("No courses found in the root");
                    Environment.Exit(0);
                }



            }

        }

        private static bool AreThereCourseFoldersInTheRoot(string username, string password)
        {
            PanoptoSessionManagement.AuthenticationInfo sessionAuthInfo = new PanoptoSessionManagement.AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };

            bool returnAreThereCoursesInTheRoot = false;
            bool lastPage = false;
            int resultsPerPage = 100;
            int page = 0;

            ISessionManagement sessionMgr = new SessionManagementClient();

            while (!lastPage)
            {
                PanoptoSessionManagement.Pagination pagination = new PanoptoSessionManagement.Pagination { MaxNumberResults = resultsPerPage, PageNumber = page };
                ListFoldersResponse response = sessionMgr.GetFoldersList(sessionAuthInfo, new ListFoldersRequest { Pagination = pagination, ParentFolderId = Guid.Empty ,SortIncreasing = true }, null);

                if (resultsPerPage * (page + 1) >= response.TotalNumberResults)
                {
                    lastPage = true;
                }

                if (response.Results.Length > 0)
                {
                    if (response.Results.Any(folder => Regex.IsMatch(folder.Name, "^[A-Z]{4}[0-9]{4}-.+$") && folder.ParentFolder.Equals(null)))
                    {
                        returnAreThereCoursesInTheRoot = true;
                        writeLine("Courses found in the root");
                        break;
                    }
                }

                page++;
            }

            return returnAreThereCoursesInTheRoot;
        }

        private static int RunCourseHierarchy(string username, string password)
        {
            // PUT YOUR AUTHENTICATION DETAILS HERE
            PanoptoSessionManagement.AuthenticationInfo sessionAuthInfo = new PanoptoSessionManagement.AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };

            PanoptoAccessManagement.AuthenticationInfo accessAuthInfo = new PanoptoAccessManagement.AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };

            bool lastPage = false;
            int resultsPerPage = 100;
            int page = 0;

            Dictionary<Guid, string> allRootFoldersDictionary = new Dictionary<Guid, string>();
            Dictionary<Guid, string> subjectFoldersDictionary = new Dictionary<Guid, string>();
            Dictionary<Guid, string> courseFoldersDictionary = new Dictionary<Guid, string>();

            ISessionManagement sessionMgr = new SessionManagementClient();
            IAccessManagement accessMgr = new AccessManagementClient();

            while (!lastPage)
            {
                PanoptoSessionManagement.Pagination pagination = new PanoptoSessionManagement.Pagination { MaxNumberResults = resultsPerPage, PageNumber = page };
                ListFoldersResponse response = sessionMgr.GetFoldersList(sessionAuthInfo, new ListFoldersRequest { Pagination = pagination, SortIncreasing = true }, null);

                if (resultsPerPage * (page + 1) >= response.TotalNumberResults)
                {
                    lastPage = true;
                }

                if (response.Results.Length > 0)
                {
                    foreach (Folder folder in response.Results)
                    {
                        if (Regex.IsMatch(folder.Name, "^[A-Z]{4}$"))
                        {
                            subjectFoldersDictionary.Add(folder.Id, folder.Name);
                        }
                        else if (Regex.IsMatch(folder.Name, "^[A-Z]{4}[0-9]{4}$"))
                        {
                            courseFoldersDictionary.Add(folder.Id, folder.Name);
                        }
                        else if (Regex.IsMatch(folder.Name, "^[A-Z]{4}[0-9]{4}-.+$") && folder.ParentFolder.Equals(null))
                        {
                            allRootFoldersDictionary.Add(folder.Id, folder.Name);
                        }
                    }
                }

                page++;
            }

            foreach (var key in allRootFoldersDictionary.Keys)
            {
                if (allRootFoldersDictionary[key].Length > 8)
                {
                    string subject = allRootFoldersDictionary[key].Substring(0, 4);
                    string course = allRootFoldersDictionary[key].Substring(0, 8);
                    Guid subjectFolder = Guid.Empty;
                    Guid courseFolder = Guid.Empty;

                    FolderAccessDetails folderAccessDetails = accessMgr.GetFolderAccessDetails(accessAuthInfo, key);

                    if (subjectFoldersDictionary.ContainsValue(subject))
                    {
                        subjectFolder = subjectFoldersDictionary.FirstOrDefault(x => x.Value == subject).Key;
                    }
                    else
                    {
                        Folder newFolder = sessionMgr.AddFolder(sessionAuthInfo, allRootFoldersDictionary[key].Substring(0, 4), null, false);
                        subjectFoldersDictionary.Add(newFolder.Id, newFolder.Name);
                        subjectFolder = newFolder.Id;
                    }

                    foreach (Guid creatorGroup in folderAccessDetails.GroupsWithCreatorAccess)
                    {
                        try
                        {
                            accessMgr.GrantGroupAccessToFolder(accessAuthInfo, subjectFolder, creatorGroup,
                                AccessRole.Viewer);
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                    foreach (Guid viewerGroup in folderAccessDetails.GroupsWithViewerAccess)
                    {
                        try
                        {
                            accessMgr.GrantGroupAccessToFolder(accessAuthInfo, subjectFolder, viewerGroup, AccessRole.Viewer);
                        }
                        catch
                        {
                            // do nothing
                        }

                    }

                    if (courseFoldersDictionary.ContainsValue(course))
                    {
                        courseFolder = courseFoldersDictionary.FirstOrDefault(x => x.Value == course).Key;
                    }
                    else
                    {
                        Folder newFolder = sessionMgr.AddFolder(sessionAuthInfo, allRootFoldersDictionary[key].Substring(0, 8), subjectFolder, false);
                        courseFoldersDictionary.Add(newFolder.Id, newFolder.Name);
                        courseFolder = newFolder.Id;
                    }

                    foreach (Guid creatorGroup in folderAccessDetails.GroupsWithCreatorAccess)
                    {
                        try
                        {
                            accessMgr.GrantGroupAccessToFolder(accessAuthInfo, courseFolder, creatorGroup, AccessRole.Creator);
                        }
                        catch
                        {
                            // do nothing
                        }

                    }

                    foreach (Guid viewerGroup in folderAccessDetails.GroupsWithViewerAccess)
                    {
                        try
                        {
                            accessMgr.GrantGroupAccessToFolder(accessAuthInfo, courseFolder, viewerGroup, AccessRole.Viewer);
                        }
                        catch
                        {

                        }

                    }

                    sessionMgr.UpdateFolderParent(sessionAuthInfo, key, courseFolder);
                }
            }

            return allRootFoldersDictionary.Count;
        }

        private static void writeLine(string text)
        {
            Console.WriteLine(text);
            Debug.WriteLine(text);
        }

    }
}
