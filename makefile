# Mesen-Embedded Makefile (macOS only)
# Simplified build for embedded emulator

CXX := clang++
CC := clang

SDL2LIB := $(shell sdl2-config --libs)
SDL2INC := $(shell sdl2-config --cflags)

MESENOS := osx
SHAREDLIB := MesenCore.dylib

MACHINE := $(shell uname -m)
ifeq ($(MACHINE),x86_64)
	MESENPLATFORM := osx-x64
	MESENFLAGS := -m64
else
	MESENPLATFORM := osx-arm64
	MESENFLAGS :=
endif

DEBUG ?= 0
ifeq ($(DEBUG),0)
	MESENFLAGS += -O3
	BUILD_TYPE := Release
else
	MESENFLAGS += -O0 -g
	BUILD_TYPE := Debug
endif

LINKOPTIONS := -framework Foundation -framework Cocoa -framework GameController -framework CoreHaptics

CXXFLAGS = -fPIC -Wall --std=c++17 $(MESENFLAGS) $(SDL2INC) -I $(realpath ./) -I $(realpath ./Core) -I $(realpath ./Utilities) -I $(realpath ./Sdl) -I $(realpath ./MacOS)
OBJCXXFLAGS = $(CXXFLAGS)
CFLAGS = -fPIC -Wall $(MESENFLAGS)

OBJFOLDER := obj.$(MESENPLATFORM)
OUTFOLDER := bin/$(MESENPLATFORM)/$(BUILD_TYPE)

PUBLISHFLAGS := -t:BundleApp -p:UseAppHost=true -p:RuntimeIdentifier=$(MESENPLATFORM) -p:SelfContained=true -p:PublishSingleFile=false

# Source files
CORESRC := $(shell find Core -name '*.cpp')
COREOBJ := $(CORESRC:.cpp=.o)

UTILSRC := $(shell find Utilities -name '*.cpp' -o -name '*.c')
UTILOBJ := $(addsuffix .o,$(basename $(UTILSRC)))

SDLSRC := $(shell find Sdl -name '*.cpp')
SDLOBJ := $(SDLSRC:.cpp=.o)

SEVENZIPSRC := $(shell find SevenZip -name '*.c')
SEVENZIPOBJ := $(SEVENZIPSRC:.c=.o)

LUASRC := $(shell find Lua -name '*.c')
LUAOBJ := $(LUASRC:.c=.o)

MACOSSRC := $(shell find MacOS -name '*.mm')
MACOSOBJ := $(MACOSSRC:.mm=.o)

DLLSRC := $(shell find InteropDLL -name '*.cpp')
DLLOBJ := $(DLLSRC:.cpp=.o)

all: ui

ui: InteropDLL/$(OBJFOLDER)/$(SHAREDLIB)
	mkdir -p $(OUTFOLDER)/Dependencies
	rm -fr $(OUTFOLDER)/Dependencies/*
	cp InteropDLL/$(OBJFOLDER)/$(SHAREDLIB) $(OUTFOLDER)/$(SHAREDLIB)
	cd UI && dotnet publish -c $(BUILD_TYPE) -r $(MESENPLATFORM)
	cd UI && dotnet publish -c $(BUILD_TYPE) $(PUBLISHFLAGS)
	# Copy MesenCore.dylib to app bundle and publish folder
	cp $(OUTFOLDER)/$(SHAREDLIB) $(OUTFOLDER)/$(MESENPLATFORM)/publish/nesplay.app/Contents/MacOS/
	cp $(OUTFOLDER)/$(SHAREDLIB) $(OUTFOLDER)/$(MESENPLATFORM)/publish/
	# Create EmbeddedRoms folder and copy ROMs and Cheats if exist
	mkdir -p $(OUTFOLDER)/$(MESENPLATFORM)/publish/nesplay.app/Contents/MacOS/EmbeddedRoms
	mkdir -p $(OUTFOLDER)/$(MESENPLATFORM)/publish/EmbeddedRoms
	-cp -r EmbeddedRoms/* $(OUTFOLDER)/$(MESENPLATFORM)/publish/nesplay.app/Contents/MacOS/EmbeddedRoms/ 2>/dev/null || true
	-cp -r EmbeddedRoms/* $(OUTFOLDER)/$(MESENPLATFORM)/publish/EmbeddedRoms/ 2>/dev/null || true

core: InteropDLL/$(OBJFOLDER)/$(SHAREDLIB)

%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@

%.o: %.cpp
	$(CXX) $(CXXFLAGS) -c $< -o $@

%.o: %.mm
	$(CXX) $(OBJCXXFLAGS) -c $< -o $@

InteropDLL/$(OBJFOLDER)/$(SHAREDLIB): $(SEVENZIPOBJ) $(LUAOBJ) $(UTILOBJ) $(COREOBJ) $(SDLOBJ) $(DLLOBJ) $(MACOSOBJ)
	mkdir -p bin
	mkdir -p InteropDLL/$(OBJFOLDER)
	$(CXX) $(CXXFLAGS) $(LINKOPTIONS) -shared -o $(SHAREDLIB) $(DLLOBJ) $(SEVENZIPOBJ) $(LUAOBJ) $(MACOSOBJ) $(UTILOBJ) $(SDLOBJ) $(COREOBJ) $(SDL2INC) -pthread $(SDL2LIB)
	mv $(SHAREDLIB) InteropDLL/$(OBJFOLDER)

run:
	$(OUTFOLDER)/$(MESENPLATFORM)/publish/Mesen

clean:
	rm -f $(COREOBJ) $(UTILOBJ) $(SDLOBJ) $(SEVENZIPOBJ) $(LUAOBJ) $(MACOSOBJ) $(DLLOBJ)
	rm -rf InteropDLL/$(OBJFOLDER)
	rm -rf bin
