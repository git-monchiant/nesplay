// Copyright (c) 2014- NESPLAY_PSP Project.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 2.0 or later versions.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License 2.0 for more details.

// A copy of the GPL 2.0 should have been included with the program.
// If not, see http://www.gnu.org/licenses/

// Official git repository and contact information can be found at
// https://github.com/hrydgard/nesplay_psp and http://www.nesplay_psp.org/.

#include <algorithm>
#include <memory>

#include "Common/Render/DrawBuffer.h"
#include "Common/UI/View.h"
#include "Common/UI/ViewGroup.h"
#include "Common/UI/Context.h"
#include "Common/UI/UIScreen.h"
#include "Common/UI/PopupScreens.h"
#include "Common/UI/Notice.h"
#include "Common/GPU/thin3d.h"

#include "Common/Data/Text/I18n.h"
#include "Common/Data/Text/Parsers.h"
#include "Common/StringUtils.h"
#include "Common/System/OSD.h"
#include "Common/System/Request.h"
#include "Common/VR/NesplayVR.h"
#include "Common/UI/AsyncImageFileView.h"

#include "Core/KeyMap.h"
#include "Core/Reporting.h"
#include "Core/Dialog/PSPSaveDialog.h"
#include "Core/SaveState.h"
#include "Core/System.h"
#include "Core/Core.h"
#include "Core/Config.h"
#include "Core/RetroAchievements.h"
#include "Core/ELF/ParamSFO.h"
#include "Core/HLE/sceDisplay.h"
#include "Core/HLE/sceUmd.h"
#include "Core/HLE/sceNet.h"
#include "Core/HLE/sceNetInet.h"
#include "Core/HLE/sceNetAdhoc.h"
#include "Core/Util/GameDB.h"
#include "Core/HLE/NetAdhocCommon.h"

#include "GPU/GPUCommon.h"
#include "GPU/GPUState.h"

#include "UI/EmuScreen.h"
#include "UI/PauseScreen.h"
#include "UI/GameSettingsScreen.h"
#include "UI/ReportScreen.h"
#include "UI/CwCheatScreen.h"
#include "UI/MainScreen.h"
#include "UI/GameScreen.h"
#include "UI/OnScreenDisplay.h"
#include "UI/GameInfoCache.h"
#include "UI/DisplayLayoutScreen.h"
#include "UI/RetroAchievementScreens.h"
#include "UI/TouchControlLayoutScreen.h"
#include "UI/BackgroundAudio.h"
#include "UI/MiscViews.h"

static void AfterSaveStateAction(SaveState::Status status, std::string_view message) {
	if (!message.empty() && (!g_Config.bDumpFrames || !g_Config.bDumpVideoOutput)) {
		g_OSD.Show(status == SaveState::Status::SUCCESS ? OSDType::MESSAGE_SUCCESS : OSDType::MESSAGE_ERROR,
			message, status == SaveState::Status::SUCCESS ? 2.0 : 5.0);
	}
}

class ScreenshotViewScreen : public UI::PopupScreen {
public:
	ScreenshotViewScreen(const Path &screenshotFilename, std::string_view saveStatePrefix, std::string_view title, int slot, Path gamePath)
		: PopupScreen(title), screenshotFilename_(screenshotFilename), saveStatePrefix_(saveStatePrefix), slot_(slot), gamePath_(gamePath), title_(title) {}   // PopupScreen will translate Back on its own

	int GetSlot() const {
		return slot_;
	}

	const char *tag() const override { return "ScreenshotView"; }

protected:
	bool FillVertical() const override { return false; }
	UI::Size PopupWidth() const override { return 500; }
	bool ShowButtons() const override { return true; }

	void CreatePopupContents(UI::ViewGroup *parent) override {
		using namespace UI;
		auto pa = GetI18NCategory(I18NCat::PAUSE);
		auto di = GetI18NCategory(I18NCat::DIALOG);

		ScrollView *scroll = new ScrollView(ORIENT_VERTICAL, new LinearLayoutParams(FILL_PARENT, WRAP_CONTENT, 1.0f));
		LinearLayout *content = new LinearLayout(ORIENT_VERTICAL);
		Margins contentMargins(10, 0);
		content->Add(new AsyncImageFileView(screenshotFilename_, IS_KEEP_ASPECT, new LinearLayoutParams(480, 272, contentMargins)))->SetCanBeFocused(false);

		GridLayoutSettings gridsettings(240, 64, 5);
		gridsettings.fillCells = true;
		GridLayout *grid = content->Add(new GridLayoutList(gridsettings, new LayoutParams(FILL_PARENT, WRAP_CONTENT)));

		Choice *back = new Choice(di->T("Back"));

		const bool hasUndo = SaveState::HasUndoSaveInSlot(saveStatePrefix_, slot_);
		const bool undoEnabled = g_Config.bEnableStateUndo;

		Choice *undoButton = nullptr;
		if (undoEnabled || hasUndo) {
			// Show the undo button if state undo is enabled in settings, OR one is available. We can load it
			// even if making new undo states is not enabled.
			Choice *undoButton = new Choice(pa->T("Undo last save"));
			undoButton->SetEnabled(hasUndo);
		}

		grid->Add(new Choice(pa->T("Save State")))->OnClick.Handle(this, &ScreenshotViewScreen::OnSaveState);
		// We can unconditionally show the load state button, because you can only pop this dialog up if a state exists.
		grid->Add(new Choice(pa->T("Load State")))->OnClick.Handle(this, &ScreenshotViewScreen::OnLoadState);
		grid->Add(new Choice(pa->T("Delete State")))->OnClick.Handle(this, &ScreenshotViewScreen::OnDeleteState);
		if (undoButton) {
			grid->Add(undoButton)->OnClick.Handle(this, &ScreenshotViewScreen::OnUndoState);
		}
		grid->Add(back)->OnClick.Handle<UIScreen>(this, &UIScreen::OnBack);

		scroll->Add(content);
		parent->Add(scroll);
	}

private:
	void OnSaveState(UI::EventParams &e);
	void OnLoadState(UI::EventParams &e);
	void OnUndoState(UI::EventParams &e);
	void OnDeleteState(UI::EventParams &e);

	Path screenshotFilename_;
	Path gamePath_;
	std::string saveStatePrefix_;
	std::string title_;
	int slot_;
};

void ScreenshotViewScreen::OnSaveState(UI::EventParams &e) {
	if (!NetworkWarnUserIfOnlineAndCantSavestate()) {
		g_Config.iCurrentStateSlot = slot_;
		SaveState::SaveSlot(saveStatePrefix_, slot_, &AfterSaveStateAction);
		TriggerFinish(DR_OK); //OK will close the pause screen as well
	}
}

void ScreenshotViewScreen::OnLoadState(UI::EventParams &e) {
	if (!NetworkWarnUserIfOnlineAndCantSavestate()) {
		g_Config.iCurrentStateSlot = slot_;
		SaveState::LoadSlot(saveStatePrefix_, slot_, &AfterSaveStateAction);
		TriggerFinish(DR_OK);
	}
}

void ScreenshotViewScreen::OnUndoState(UI::EventParams &e) {
	if (!NetworkWarnUserIfOnlineAndCantSavestate()) {
		SaveState::UndoSaveSlot(saveStatePrefix_, slot_);
		TriggerFinish(DR_CANCEL);
	}
}

void ScreenshotViewScreen::OnDeleteState(UI::EventParams &e) {
	auto di = GetI18NCategory(I18NCat::DIALOG);

	std::shared_ptr<GameInfo> info = g_gameInfoCache->GetInfo(NULL, gamePath_, GameInfoFlags::PARAM_SFO);

	std::string_view title = di->T("Delete");
	std::string message = std::string(di->T("DeleteConfirmSaveState")) + "\n\n" + info->GetTitle() + " (" + info->id + ")";
	message += "\n\n" + title_;

	// TODO: Also show the screenshot on the confirmation screen?

	screenManager()->push(new UI::MessagePopupScreen(title, message, di->T("Delete"), di->T("Cancel"), [this](bool result) {
		if (result) {
			SaveState::DeleteSlot(saveStatePrefix_, slot_);
			TriggerFinish(DR_CANCEL);
		}
	}));
}

class SaveSlotView : public UI::LinearLayout {
public:
	SaveSlotView(std::string_view saveStatePrefix, int slot, UI::LayoutParams *layoutParams = nullptr);

	void GetContentDimensions(const UIContext &dc, float &w, float &h) const override {
		w = 500; h = 90;
	}

	void Draw(UIContext &dc) override;

	int GetSlot() const {
		return slot_;
	}

	Path GetScreenshotFilename() const {
		return screenshotFilename_;
	}

	std::string GetScreenshotTitle() const {
		return SaveState::GetSlotDateAsString(saveStatePrefix_, slot_);
	}

	UI::Event OnStateLoaded;
	UI::Event OnStateSaved;
	UI::Event OnScreenshotClicked;
	UI::Event OnSelected;

private:
	void OnSaveState(UI::EventParams &e);
	void OnLoadState(UI::EventParams &e);

	UI::Button *saveStateButton_ = nullptr;
	UI::Button *loadStateButton_ = nullptr;

	int slot_;
	std::string saveStatePrefix_;
	Path screenshotFilename_;
};

SaveSlotView::SaveSlotView(std::string_view saveStatePrefix, int slot, UI::LayoutParams *layoutParams) : UI::LinearLayout(ORIENT_HORIZONTAL, layoutParams), slot_(slot), saveStatePrefix_(saveStatePrefix) {
	using namespace UI;

	screenshotFilename_ = SaveState::GenerateSaveSlotPath(saveStatePrefix_, slot, SaveState::SCREENSHOT_EXTENSION);

	std::string number = StringFromFormat("%d", slot + 1);
	Add(new Spacer(5));

	// TEMP HACK: use some other view, like a Choice, themed differently to enable keyboard access to selection.
	ClickableTextView *numberView = Add(new ClickableTextView(number, new LinearLayoutParams(40.0f, WRAP_CONTENT, 0.0f, Gravity::G_VCENTER)));
	numberView->SetBig(true);
	numberView->OnClick.Add([this](UI::EventParams &e) {
		e.v = this;
		OnSelected.Trigger(e);
	});

	AsyncImageFileView *fv = Add(new AsyncImageFileView(screenshotFilename_, IS_DEFAULT, new UI::LayoutParams(82 * 2, 47 * 2)));

	auto pa = GetI18NCategory(I18NCat::PAUSE);
	auto sy = GetI18NCategory(I18NCat::SYSTEM);

	LinearLayout *lines = new LinearLayout(ORIENT_VERTICAL, new LinearLayoutParams(WRAP_CONTENT, WRAP_CONTENT));
	lines->SetSpacing(2.0f);

	Add(lines);

	LinearLayout *buttons = new LinearLayout(ORIENT_HORIZONTAL, new LinearLayoutParams(WRAP_CONTENT, WRAP_CONTENT));
	buttons->SetSpacing(10.0f);

	lines->Add(buttons);

	saveStateButton_ = buttons->Add(new Button(pa->T("Save State"), new LinearLayoutParams(0.0, Gravity::G_VCENTER)));
	saveStateButton_->OnClick.Handle(this, &SaveSlotView::OnSaveState);

	fv->OnClick.Add([this](UI::EventParams &e) {
		e.v = this;
		OnScreenshotClicked.Trigger(e);
	});

	if (SaveState::HasSaveInSlot(saveStatePrefix_, slot_)) {
		if (!Achievements::HardcoreModeActive()) {
			loadStateButton_ = buttons->Add(new Button(pa->T("Load State"), new LinearLayoutParams(0.0, Gravity::G_VCENTER)));
			loadStateButton_->OnClick.Handle(this, &SaveSlotView::OnLoadState);
		}

		std::string dateStr = SaveState::GetSlotDateAsString(saveStatePrefix_, slot_);

		if (slot_ == g_Config.iAutoLoadSaveState - 3) {
			dateStr += " (" + std::string(sy->T("Auto load savestate")) + ")";
		}

		if (!dateStr.empty()) {
			TextView *dateView = new TextView(dateStr, new LinearLayoutParams(0.0, Gravity::G_VCENTER));
			dateView->SetSmall(true);
			lines->Add(dateView)->SetShadow(true);
		}
	} else {
		fv->SetFilename(Path());
	}
}

void SaveSlotView::Draw(UIContext &dc) {
	if (g_Config.iCurrentStateSlot == slot_) {
		dc.FillRect(UI::Drawable(0x70000000), GetBounds().Expand(3));
		dc.FillRect(UI::Drawable(0x70FFFFFF), GetBounds().Expand(3));
	}
	UI::LinearLayout::Draw(dc);
}

void SaveSlotView::OnLoadState(UI::EventParams &e) {
	if (!NetworkWarnUserIfOnlineAndCantSavestate()) {
		g_Config.iCurrentStateSlot = slot_;
		SaveState::LoadSlot(saveStatePrefix_, slot_, &AfterSaveStateAction);
		UI::EventParams e2{};
		e2.v = this;
		OnStateLoaded.Trigger(e2);
	}
}

void SaveSlotView::OnSaveState(UI::EventParams &e) {
	if (!NetworkWarnUserIfOnlineAndCantSavestate()) {
		g_Config.iCurrentStateSlot = slot_;
		SaveState::SaveSlot(saveStatePrefix_, slot_, &AfterSaveStateAction);
		UI::EventParams e2{};
		e2.v = this;
		OnStateSaved.Trigger(e2);
	}
}

void GamePauseScreen::update() {
	UpdateUIState(UISTATE_PAUSEMENU);
	UIScreen::update();

	if (finishNextFrame_) {
		TriggerFinish(finishNextFrameResult_);
		finishNextFrame_ = false;
	}

	const bool networkConnected = IsNetworkConnected();
	const InfraDNSConfig &dnsConfig = GetInfraDNSConfig();
	if (g_netInited != lastNetInited_ || netInetInited != lastNetInetInited_ || lastAdhocServerConnected_ != g_adhocServerConnected || lastOnline_ != networkConnected || lastDNSConfigLoaded_ != dnsConfig.loaded) {
		INFO_LOG(Log::sceNet, "Network status changed (or pause dialog just popped up), recreating views");
		RecreateViews();
		lastNetInetInited_ = netInetInited;
		lastNetInited_ = g_netInited;
		lastAdhocServerConnected_ = g_adhocServerConnected;
		lastOnline_ = networkConnected;
		lastDNSConfigLoaded_ = dnsConfig.loaded;
	}

	if (playButton_) {
		const bool mustRunBehind = MustRunBehind();
		playButton_->SetVisibility(mustRunBehind ? UI::V_GONE : UI::V_VISIBLE);
	}

	SetVRAppMode(VRAppMode::VR_MENU_MODE);
}

GamePauseScreen::GamePauseScreen(const Path &filename, bool bootPending)
	: UIBaseDialogScreen(filename), bootPending_(bootPending) {
	// So we can tell if something blew up while on the pause screen.
	std::string assertStr = "PauseScreen: " + filename.GetFilename();
	SetExtraAssertInfo(assertStr.c_str());
	saveStatePrefix_ = SaveState::GetGamePrefix(g_paramSFO);
	SaveState::Rescan(saveStatePrefix_);
	g_controlMapper.AddListener(this);
	createdTime_ = time_now_d();
}

GamePauseScreen::~GamePauseScreen() {
	g_controlMapper.RemoveListener(this);
	__DisplaySetWasPaused();
}

bool GamePauseScreen::UnsyncKey(const KeyInput &key) {
	int retval = UIScreen::UnsyncKey(key);
	bool pauseTrigger = false;
	return retval || g_controlMapper.Key(key, &pauseTrigger);
}

void GamePauseScreen::UnsyncAxis(const AxisInput *axes, size_t count) {
	UIScreen::UnsyncAxis(axes, count);
	g_controlMapper.Axis(axes, count);
}

void GamePauseScreen::OnVKey(VirtKey virtualKeyCode, bool down) {
	// Simple de-bounce using createdTime_, just to be safe.
	if (down && virtualKeyCode == VIRTKEY_PAUSE && time_now_d() > createdTime_ + 0.1) {
		finishNextFrame_ = true;
		finishNextFrameResult_ = DR_BACK;
	}
}

void GamePauseScreen::CreateSavestateControls(UI::LinearLayout *leftColumnItems) {
	auto pa = GetI18NCategory(I18NCat::PAUSE);

	using namespace UI;

	leftColumnItems->SetSpacing(10.0);
	for (int i = 0; i < g_Config.iSaveStateSlotCount; i++) {
		SaveSlotView *slot = leftColumnItems->Add(new SaveSlotView(saveStatePrefix_, i, new LinearLayoutParams(FILL_PARENT, WRAP_CONTENT, Gravity::G_HCENTER, Margins(0,0,0,0))));
		slot->OnStateLoaded.Handle(this, &GamePauseScreen::OnState);
		slot->OnStateSaved.Handle(this, &GamePauseScreen::OnState);
		slot->OnScreenshotClicked.Add([this](UI::EventParams &e) {
			SaveSlotView *v = static_cast<SaveSlotView *>(e.v);
			int slot = v->GetSlot();
			g_Config.iCurrentStateSlot = v->GetSlot();
			if (SaveState::HasSaveInSlot(saveStatePrefix_, slot)) {
				Path fn = v->GetScreenshotFilename();
				std::string title = v->GetScreenshotTitle();
				Screen *screen = new ScreenshotViewScreen(fn, saveStatePrefix_, title, v->GetSlot(), gamePath_);
				screenManager()->push(screen);
			}
		});
		slot->OnSelected.Add([this](UI::EventParams &e) {
			SaveSlotView *v = static_cast<SaveSlotView *>(e.v);
			g_Config.iCurrentStateSlot = v->GetSlot();
			RecreateViews();
		});
	}
	leftColumnItems->Add(new Spacer(0.0));

	LinearLayout *buttonRow = leftColumnItems->Add(new LinearLayout(ORIENT_HORIZONTAL, new LinearLayoutParams(Margins(10, 0, 0, 0))));
	if (g_Config.bEnableStateUndo && !Achievements::HardcoreModeActive() && NetworkAllowSaveState()) {
		UI::Choice *loadUndoButton = buttonRow->Add(new Choice(pa->T("Undo last load"), ImageID("I_NAVIGATE_BACK")));
		loadUndoButton->SetEnabled(SaveState::HasUndoLoad(saveStatePrefix_));
		loadUndoButton->OnClick.Handle(this, &GamePauseScreen::OnLoadUndo);

		UI::Choice *saveUndoButton = buttonRow->Add(new Choice(pa->T("Undo last save"), ImageID("I_NAVIGATE_BACK")));
		saveUndoButton->SetEnabled(SaveState::HasUndoLastSave(saveStatePrefix_));
		saveUndoButton->OnClick.Handle(this, &GamePauseScreen::OnLastSaveUndo);
	}

	if (g_Config.iRewindSnapshotInterval > 0 && !Achievements::HardcoreModeActive() && NetworkAllowSaveState()) {
		UI::Choice *rewindButton = buttonRow->Add(new Choice(pa->T("Rewind"), ImageID("I_REWIND")));
		rewindButton->SetEnabled(SaveState::CanRewind());
		rewindButton->OnClick.Handle(this, &GamePauseScreen::OnRewind);
	}
}

UI::Margins GamePauseScreen::RootMargins() const {
	return UI::Margins(0);
}

void GamePauseScreen::CreateViews() {
	using namespace UI;

	auto pa = GetI18NCategory(I18NCat::PAUSE);

	// Left sidebar layout overlaying the game
	root_ = new AnchorLayout(new LayoutParams(FILL_PARENT, FILL_PARENT));

	// Semi-transparent dim overlay covering the full screen
	AnchorLayout *dimOverlay = new AnchorLayout(new AnchorLayoutParams(FILL_PARENT, FILL_PARENT));
	dimOverlay->SetBG(Drawable(0x80000000));
	root_->Add(dimOverlay);

	// Left sidebar panel
	LinearLayout *sidebar = new LinearLayout(ORIENT_VERTICAL, new AnchorLayoutParams(300, FILL_PARENT, 0, 0, NONE, 0));
	sidebar->SetBG(Drawable(0xF0282828));
	root_->Add(sidebar);

	// Game title at top
	std::string title;
	std::vector<GameDBInfo> dbInfos;
	if (g_gameDB.GetGameInfos(g_paramSFO.GetDiscID(), &dbInfos)) {
		title = dbInfos[0].title;
	} else {
		title = g_paramSFO.GetValueString("TITLE");
	}
	TextView *titleView = sidebar->Add(new TextView(title, FLAG_WRAP_TEXT, false, new LinearLayoutParams(FILL_PARENT, WRAP_CONTENT, Margins(20, 30, 20, 10))));
	titleView->SetShadow(true);

	sidebar->Add(new Spacer(20.0f));

	// Menu items
	Choice *continueChoice = sidebar->Add(new Choice(pa->T("Continue"), ImageID("I_PLAY")));
	continueChoice->OnClick.Handle<UIScreen>(this, &UIScreen::OnBack);
	root_->SetDefaultFocusView(continueChoice);

	sidebar->Add(new Choice(pa->T("Settings"), ImageID("I_GEAR")))->OnClick.Handle(this, &GamePauseScreen::OnGameSettings);

	sidebar->Add(new Spacer(1.0f, new LinearLayoutParams(1.0f)));

	Choice *exit;
	if (g_Config.bPauseMenuExitsEmulator) {
		auto mm = GetI18NCategory(I18NCat::MAINMENU);
		exit = sidebar->Add(new Choice(mm->T("Exit"), ImageID("I_EXIT")));
	} else {
		exit = sidebar->Add(new Choice(pa->T("Exit to menu"), ImageID("I_EXIT")));
	}
	exit->OnClick.Handle(this, &GamePauseScreen::OnExit);
	exit->SetEnabled(!bootPending_);

	sidebar->Add(new Spacer(20.0f));

	playButton_ = nullptr;
}

void GamePauseScreen::ShowContextMenu(UI::View *menuButton, bool portrait) {
	using namespace UI;
	PopupCallbackScreen *contextMenu = new UI::PopupCallbackScreen([this, portrait](UI::ViewGroup *parent) {
		auto di = GetI18NCategory(I18NCat::DIALOG);
		parent->Add(new Choice(di->T("Reset")))->OnClick.Add([this](UI::EventParams &e) {
			std::string confirmMessage = GetConfirmExitMessage();
			if (!confirmMessage.empty()) {
				auto di = GetI18NCategory(I18NCat::DIALOG);
				screenManager()->push(new UI::MessagePopupScreen(di->T("Reset"), confirmMessage, di->T("Reset"), di->T("Cancel"), [this](bool result) {
					if (result) {
						System_PostUIMessage(UIMessage::REQUEST_GAME_RESET);
						finishNextFrameResult_ = DR_BACK;  // resume
						finishNextFrame_ = true;
					}
				}));
			} else {
				System_PostUIMessage(UIMessage::REQUEST_GAME_RESET);
				finishNextFrameResult_ = DR_BACK;  // resume
				finishNextFrame_ = true;
			}
		});

		if (portrait) {
			AddExtraOptions(parent);
		}
	}, menuButton);
	screenManager()->push(contextMenu);
}

void GamePauseScreen::AddExtraOptions(UI::ViewGroup *parent) {
	using namespace UI;
	// Add some other options that are removed from the main screen in portrait mode.
	if (Reporting::IsSupported() && g_paramSFO.GetValueString("DISC_ID").size()) {
		auto rp = GetI18NCategory(I18NCat::REPORTING);
		parent->Add(new Choice(rp->T("ReportButton", "Report Feedback")))->OnClick.Handle(this, &GamePauseScreen::OnReportFeedback);
	}
}

void GamePauseScreen::OnGameSettings(UI::EventParams &e) {
	screenManager()->push(new GameSettingsScreen(gamePath_));
}

void GamePauseScreen::OnState(UI::EventParams &e) {
	TriggerFinish(DR_CANCEL);
}

void GamePauseScreen::dialogFinished(const Screen *dialog, DialogResult dr) {
	std::string tag = dialog->tag();
	if (tag == "ScreenshotView" && dr == DR_OK) {
		finishNextFrame_ = true;
	} else {
		if (tag == "Game") {
			g_BackgroundAudio.SetGame(Path());
		} else if (tag != "Prompt" && tag != "ContextMenuPopup") {
			// There may have been changes to our savestates, so let's recreate.
			RecreateViews();
		}
	}
}

int GetUnsavedProgressSeconds() {
	const double timeSinceSaveState = SaveState::SecondsSinceLastSavestate();
	const double timeSinceGameSave = SecondsSinceLastGameSave();

	return (int)std::min(timeSinceSaveState, timeSinceGameSave);
}

// If empty, no confirmation dialog should be shown.
std::string GetConfirmExitMessage() {
	std::string confirmMessage;

	int unsavedSeconds = GetUnsavedProgressSeconds();

	// If RAIntegration has dirty info, ask for confirmation.
	if (Achievements::RAIntegrationDirty()) {
		auto ac = GetI18NCategory(I18NCat::ACHIEVEMENTS);
		confirmMessage = ac->T("You have unsaved RAIntegration changes.");
		confirmMessage += '\n';
	}

	if (coreState == CORE_RUNTIME_ERROR) {
		// The game crashed, or similar. Don't bother checking for timeout or network.
		return confirmMessage;
	}

	if (IsNetworkConnected()) {
		auto nw = GetI18NCategory(I18NCat::NETWORKING);
		confirmMessage += nw->T("Network connected");
		confirmMessage += '\n';
	} else if (g_Config.iAskForExitConfirmationAfterSeconds > 0 && unsavedSeconds > g_Config.iAskForExitConfirmationAfterSeconds) {
		if (PSP_CoreParameter().fileType == IdentifiedFileType::NESPLAY_PSP_GE_DUMP) {
			// No need to ask for this type of confirmation for dumps.
			return confirmMessage;
		}
		auto di = GetI18NCategory(I18NCat::DIALOG);
		confirmMessage = ApplySafeSubstitutions(di->T("You haven't saved your progress for %1."), NiceTimeFormat((int)unsavedSeconds));
		confirmMessage += '\n';
	}

	return confirmMessage;
}

void GamePauseScreen::OnExit(UI::EventParams &e) {
	std::string confirmExitMessage = GetConfirmExitMessage();

	if (!confirmExitMessage.empty()) {
		auto di = GetI18NCategory(I18NCat::DIALOG);
		std::string_view title = di->T("Are you sure you want to exit?");
		screenManager()->push(new UI::MessagePopupScreen(title, confirmExitMessage, di->T("Exit"), di->T("Cancel"), [this](bool result) {
			if (result) {
				if (g_Config.bPauseMenuExitsEmulator) {
					System_ExitApp();
				} else {
					finishNextFrameResult_ = DR_OK;  // exit game
					finishNextFrame_ = true;
				}
			}
		}));
	} else {
		if (g_Config.bPauseMenuExitsEmulator) {
			System_ExitApp();
		} else {
			TriggerFinish(DR_OK);
		}
	}
}

void GamePauseScreen::OnReportFeedback(UI::EventParams &e) {
	screenManager()->push(new ReportScreen(gamePath_));
}

void GamePauseScreen::OnRewind(UI::EventParams &e) {
	SaveState::Rewind(&AfterSaveStateAction);

	TriggerFinish(DR_CANCEL);
}

void GamePauseScreen::OnLoadUndo(UI::EventParams &e) {
	SaveState::UndoLoad(saveStatePrefix_, &AfterSaveStateAction);

	TriggerFinish(DR_CANCEL);
}

void GamePauseScreen::OnLastSaveUndo(UI::EventParams &e) {
	SaveState::UndoLastSave(saveStatePrefix_);

	RecreateViews();
}

void GamePauseScreen::OnCreateConfig(UI::EventParams &e) {
	std::shared_ptr<GameInfo> info = g_gameInfoCache->GetInfo(NULL, gamePath_, GameInfoFlags::PARAM_SFO);
	if (info->Ready(GameInfoFlags::PARAM_SFO)) {
		std::string gameId = info->id;
		g_Config.CreateGameConfig(gameId);
		g_Config.SaveGameConfig(gameId, info->GetTitle());
		if (info) {
			info->hasConfig = true;
		}
		RecreateViews();
	}
}

void GamePauseScreen::OnDeleteConfig(UI::EventParams &e) {
	auto di = GetI18NCategory(I18NCat::DIALOG);
	const bool trashAvailable = System_GetPropertyBool(SYSPROP_HAS_TRASH_BIN);
	screenManager()->push(
		new UI::MessagePopupScreen(di->T("Delete"), di->T("DeleteConfirmGameConfig", "Do you really want to delete the settings for this game?"),
			trashAvailable ? di->T("Move to trash") : di->T("Delete"), di->T("Cancel"), [this](bool yes) {
		if (!yes) {
			return;
		}
		std::shared_ptr<GameInfo> info = g_gameInfoCache->GetInfo(NULL, gamePath_, GameInfoFlags::PARAM_SFO);
		if (info->Ready(GameInfoFlags::PARAM_SFO)) {
			g_Config.UnloadGameConfig();
			g_Config.DeleteGameConfig(info->id);
			info->hasConfig = false;
			screenManager()->RecreateAllViews();
		}
	}));
}
