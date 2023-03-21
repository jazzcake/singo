include $(CLEAR_VARS)

# override strip command to strip all symbols from output library; no need to ship with those..
# cmd-strip = $(TOOLCHAIN_PREFIX)strip $1 

LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libjuice
LOCAL_CFLAGS    := -Werror
LOCAL_SRC_FILES := addr.c \
	agent.c \
	base64.c \
	conn.c \
	conn_mux.c \
	conn_poll.c \
	conn_thread.c \
	const_time.c \
	crc32.c \
	hash.c \
	hmac.c \
	ice.c \
	juice.c \
	log.c \
	random.c \
	server.c \
	stun.c \
	timestamp.c \
	turn.c \
	udp.c
LOCAL_LDLIBS    := -llog

include $(BUILD_SHARED_LIBRARY)
