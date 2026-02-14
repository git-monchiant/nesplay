//
//  DarwinFileSystemServices.h
//  NESPLAY_PSP
//
//  Created by Serena on 20/01/2023.
//

#pragma once

#include "nesplay_psp_config.h"
#include "Common/File/Path.h"
#include "Common/System/Request.h"

#define PreferredMemoryStickUserDefaultsKey "UserPreferredMemoryStickDirectoryPath"

typedef std::function<void (bool, Path)> DarwinDirectoryPanelCallback;

/// A Class providing help functions to work with the FileSystem
/// on Darwin platforms.
class DarwinFileSystemServices {
public:
	/// Present a panel to choose a file or directory.
	static void presentDirectoryPanel(
		DarwinDirectoryPanelCallback panelCallback,
		bool allowFiles = false,
		bool allowDirectories = true,
		BrowseFileType fileType = BrowseFileType::ANY);

	static Path appropriateMemoryStickDirectoryToUse();
	static void setUserPreferredMemoryStickDirectory(Path);
	static Path defaultMemoryStickPath();

	static void ClearDelegate();
private:
#if NESPLAY_PSP_PLATFORM(IOS)
	// iOS only, needed for UIDocumentPickerViewController
	static void *__pickerDelegate;
#endif // NESPLAY_PSP_PLATFORM(IOS)
};

void RestartMacApp();
