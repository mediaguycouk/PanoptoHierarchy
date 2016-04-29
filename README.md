# PanoptoHierarchy
Looks for folders in the Panopto root in the format ABCD1234-12345-15-16... and creates an ABCD and ABCD/ABCD1234 folder with permissions that allow for content to be rolled over in Blackboard without copying files between Panopto folders.

Put your server in app.config
Run as a scheduled task via PanoptoHierarchy.exe /u [admin username] /p [admin password]

Based on 
http://www.mediaguy.co.uk/panopto-api/panopto-api-103-organise-course-folders-into-subfolders/
and
http://www.mediaguy.co.uk/panopto-api/panopto-api-104-adding-group-permissions-to-the-parent-folders/
