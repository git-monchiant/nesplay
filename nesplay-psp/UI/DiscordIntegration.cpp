
#include <ctime>
#include <string>

#include "nesplay_psp_config.h"
#include "Common/Log.h"
#include "Core/Config.h"
#include "UI/DiscordIntegration.h"
#include "Common/Data/Text/I18n.h"
#include "Common/System/System.h"

#if (NESPLAY_PSP_PLATFORM(WINDOWS) || NESPLAY_PSP_PLATFORM(MAC) || NESPLAY_PSP_PLATFORM(LINUX)) && !NESPLAY_PSP_PLATFORM(ANDROID) && !NESPLAY_PSP_PLATFORM(UWP)

#ifdef _MSC_VER
#define ENABLE_DISCORD
#elif USE_DISCORD
#define ENABLE_DISCORD
#endif

#else

// TODO

#endif

#ifdef ENABLE_DISCORD
#include "ext/discord-rpc/include/discord_rpc.h"
#endif

// TODO: Enable on more platforms. Make optional.

Discord g_Discord;

static const char *nesplay_psp_app_id = "423397985041383434";
static const char *nesplay_psp_app_id_gold = "1471464214710190152";

#ifdef ENABLE_DISCORD
// No context argument? What?
static void handleDiscordError(int errCode, const char *message) {
	ERROR_LOG(Log::System, "Discord error code %d: '%s'", errCode, message);
}
#endif

Discord::~Discord() {
	if (initialized_) {
		ERROR_LOG(Log::System, "Discord destructor running though g_Discord.Shutdown() has not been called.");
	}
}

bool Discord::IsAvailable() {
#ifdef ENABLE_DISCORD
	return true;
#else
	return false;
#endif
}

bool Discord::IsEnabled() const {
	return g_Config.bDiscordRichPresence;
}

void Discord::Init() {
	_assert_(IsEnabled());
	_assert_(!initialized_);

#ifdef ENABLE_DISCORD
	DiscordEventHandlers eventHandlers{};
	eventHandlers.errored = &handleDiscordError;

	const char *appId = nesplay_psp_app_id;
	if (System_GetPropertyBool(SYSPROP_APP_GOLD)) {
		appId = nesplay_psp_app_id_gold;
	}

	Discord_Initialize(appId, &eventHandlers, 0, nullptr);
	INFO_LOG(Log::System, "Discord connection initialized");
#endif

	initialized_ = true;
}

void Discord::Shutdown() {
	if (initialized_) {
#ifdef ENABLE_DISCORD
		Discord_Shutdown();
#endif
		initialized_ = false;
	}
}

void Discord::Update() {
	if (!IsEnabled()) {
		if (initialized_) {
			Shutdown();
		}
		return;
	} else {
		if (!initialized_) {
			Init();
		}
	}

#ifdef ENABLE_DISCORD
#ifdef DISCORD_DISABLE_IO_THREAD
	Discord_UpdateConnection();
#endif
	Discord_RunCallbacks();
#endif
}

void Discord::SetPresenceGame(std::string_view gameTitle) {
	if (!IsEnabled())
		return;
	
	if (!initialized_) {
		Init();
	}

#ifdef ENABLE_DISCORD
	auto sc = GetI18NCategory(I18NCat::SCREEN);
	std::string title(gameTitle);
	DiscordRichPresence discordPresence{};
	discordPresence.state = title.c_str();
	discordPresence.details = sc->T_cstr("Playing");
	discordPresence.startTimestamp = time(0);
	discordPresence.largeImageText = "nesplay-psp PSP emulator";
	discordPresence.largeImageKey = System_GetPropertyBool(SYSPROP_APP_GOLD) ? "icon_gold_png" : "icon_regular_png";
	Discord_UpdatePresence(&discordPresence);
#endif
}

void Discord::SetPresenceMenu() {
	if (!IsEnabled())
		return;

	if (!initialized_) {
		Init();
	}

#ifdef ENABLE_DISCORD
	auto sc = GetI18NCategory(I18NCat::SCREEN);

	DiscordRichPresence discordPresence{};
	discordPresence.state = sc->T_cstr("In menu");
	discordPresence.details = "";
	discordPresence.startTimestamp = time(0);
	discordPresence.largeImageText = "nesplay-psp PSP emulator";
	discordPresence.largeImageKey = System_GetPropertyBool(SYSPROP_APP_GOLD) ? "icon_gold_png" : "icon_regular_png";
#endif
}

void Discord::ClearPresence() {
	if (!IsEnabled() || !initialized_)
		return;

#ifdef ENABLE_DISCORD
	Discord_ClearPresence();
#endif
}
