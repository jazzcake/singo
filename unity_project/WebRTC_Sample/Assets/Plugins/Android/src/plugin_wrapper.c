#include "plugin_wrapper.h"
#include "log.h"

#include <assert.h>
#include <malloc.h>
#include <memory.h>
#include <stdio.h>

// definitions & constants
#define MAX_AGENT_WRAPPER_DATAS 		5
#define MAX_TURN_SERVER 				10

const JUICE_AGENT_HANDLE INVALID_JUICE_AGENT_HANDLE = -1;

// event handler extern
extern void on_state_changed(juice_agent_t* agent, juice_state_t state, void* user_ptr);
extern void on_candidate(juice_agent_t* agent, const char* sdp, void* user_ptr);
extern void on_gathering_done(juice_agent_t* agent, void* user_ptr);
extern void on_recv(juice_agent_t *agent, const char *data, size_t size, void *user_ptr);

// 전역으로 핸들 - juice_agent_t*를 매칭할 정보를 설정한다.
typedef struct juice_wrapper_data {
	JUICE_AGENT_HANDLE handle;

	juice_config_t config;
	juice_turn_server_t turn_server[MAX_TURN_SERVER];
	juice_agent_t* agent;

	juice_wrapper_on_state_changed callback_on_state_changed;
	juice_wrapper_on_candidate callback_on_candidate;
	juice_wrapper_on_gathering_done callback_on_gathering_done;
	juice_wrapper_on_recv callback_on_recv;

} juice_wrapper_data_t;

typedef juice_wrapper_data_t* lp_juice_wrapper_data;

static lp_juice_wrapper_data* g_juice_wrapper_datas = NULL;
static int juice_wrapper_data_count = 0;

// private methods
lp_juice_wrapper_data juice_wrapper_find_data(juice_agent_t *agent) {
	for (int i = 0; i < juice_wrapper_data_count; i++) {
		if (g_juice_wrapper_datas[i] == NULL)
			continue;

		if (g_juice_wrapper_datas[i]->agent == agent)
			return g_juice_wrapper_datas[i];
	}

	return NULL;
}

JUICE_AGENT_HANDLE juice_wrapper_find_handle(juice_agent_t* agent) {
	for (int i = 0; i < juice_wrapper_data_count; i++) {
		if (g_juice_wrapper_datas[i] == NULL)
			continue;

		if (g_juice_wrapper_datas[i]->agent == agent)
			return i;
	}

	return INVALID_JUICE_AGENT_HANDLE;
}

// implementations

JUICE_EXPORT void juice_wrapper_init(int agent_count) {
	assert(agent_count > 0);

	g_juice_wrapper_datas = (lp_juice_wrapper_data*)calloc(agent_count, sizeof(lp_juice_wrapper_data));
	juice_wrapper_data_count = agent_count;

	for (int i = 0; i < agent_count; i++)
		g_juice_wrapper_datas[i] = NULL;
}

JUICE_EXPORT JUICE_AGENT_HANDLE juice_wrapper_assign_agent() {
	for (int i = 0; i < juice_wrapper_data_count; i++)
	{
		if (g_juice_wrapper_datas[i] != NULL)
			continue;

		lp_juice_wrapper_data wrapper_data = calloc(1, sizeof(juice_wrapper_data_t));

		// 초기화
		wrapper_data->handle = INVALID_JUICE_AGENT_HANDLE;
		wrapper_data->callback_on_state_changed = NULL;
		wrapper_data->callback_on_candidate = NULL;
		wrapper_data->callback_on_gathering_done = NULL;
		wrapper_data->callback_on_recv = NULL;
		wrapper_data->agent = NULL;

		memset(&(wrapper_data->config), 0, sizeof(juice_config_t));

		for (int j = 0; j < MAX_TURN_SERVER; j++)
			memset(&wrapper_data->turn_server[j], 0, sizeof(juice_turn_server_t));

		wrapper_data->handle = i;

		g_juice_wrapper_datas[i] = wrapper_data;
		return wrapper_data->handle;
	}

	return INVALID_JUICE_AGENT_HANDLE;
}

JUICE_EXPORT void juice_wrapper_set_stun(JUICE_AGENT_HANDLE handle, const char *host, int port) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];

	wrapper_data->config.stun_server_host = host;
	wrapper_data->config.stun_server_port = port;
}

JUICE_EXPORT void juice_wrapper_set_turn(JUICE_AGENT_HANDLE handle, int turn_server_index,
                                   const char *host, int port, const char *username,
                                   const char *password) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];

	wrapper_data->turn_server[turn_server_index].host = host;
	wrapper_data->turn_server[turn_server_index].port = port;
	wrapper_data->turn_server[turn_server_index].username = username;
	wrapper_data->turn_server[turn_server_index].password = password;
}

JUICE_EXPORT void juice_wrapper_local_port_range(JUICE_AGENT_HANDLE handle, int begin_port,
                                            int end_port) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];

	wrapper_data->config.local_port_range_begin = begin_port;
	wrapper_data->config.local_port_range_end = end_port;
}

JUICE_EXPORT void juice_wrapper_set_callbacks(JUICE_AGENT_HANDLE handle,
	juice_wrapper_on_state_changed fn_on_state_changed,
	juice_wrapper_on_candidate fn_on_candidate,	
	juice_wrapper_on_gathering_done fn_on_gathering_done,
	juice_wrapper_on_recv fn_on_recv) {

	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];

	wrapper_data->callback_on_state_changed = fn_on_state_changed;
	wrapper_data->callback_on_candidate = fn_on_candidate;
	wrapper_data->callback_on_gathering_done = fn_on_gathering_done;
	wrapper_data->callback_on_recv = fn_on_recv;
}


JUICE_EXPORT int juice_wrapper_create_agent(JUICE_AGENT_HANDLE handle) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];

	wrapper_data->config.user_ptr = NULL;
	wrapper_data->config.cb_state_changed = on_state_changed;
	wrapper_data->config.cb_candidate = on_candidate;
	wrapper_data->config.cb_gathering_done = on_gathering_done;
	wrapper_data->config.cb_recv = on_recv;
	wrapper_data->config.user_ptr = NULL;

	int turn_count = 0;

	for (int i = 0; i < MAX_TURN_SERVER; i++) {
		if (wrapper_data->turn_server[i].host != NULL)
			turn_count += 1;
		else {
			break;
		}
	}

	wrapper_data->config.turn_servers = wrapper_data->turn_server;
	wrapper_data->config.turn_servers_count = turn_count;

	wrapper_data->agent = juice_create(&wrapper_data->config);
	if (wrapper_data->agent == NULL)
		return JUICE_ERR_FAILED;

	return JUICE_ERR_SUCCESS;
}

JUICE_EXPORT int juice_wrapper_get_local_description(JUICE_AGENT_HANDLE handle, char *buffer,
                                               size_t size) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_get_local_description(wrapper_data->agent, buffer, size);
}

JUICE_EXPORT int juice_wrapper_set_remote_description(JUICE_AGENT_HANDLE handle,
                                                      const char *buffer) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_set_remote_description(wrapper_data->agent, buffer);
}

JUICE_EXPORT int juice_wrapper_gather_candidates(JUICE_AGENT_HANDLE handle) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_gather_candidates(wrapper_data->agent);
}

JUICE_EXPORT int juice_wrapper_add_remote_candidate(JUICE_AGENT_HANDLE handle, const char *sdp) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_add_remote_candidate(wrapper_data->agent, sdp);
}

JUICE_EXPORT int juice_wrapper_set_remote_gathering_done(JUICE_AGENT_HANDLE handle) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_set_remote_gathering_done(wrapper_data->agent);
}

JUICE_EXPORT int juice_wrapper_get_selected_candidates(JUICE_AGENT_HANDLE handle, char *local,
                                                 size_t local_size, char *remote,
                                                 size_t remote_size) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_get_selected_candidates(wrapper_data->agent, local, local_size, remote, remote_size);
}

JUICE_EXPORT int juice_wrapper_get_selected_addresses(JUICE_AGENT_HANDLE handle, char* local,
	size_t local_size, char* remote,
	size_t remote_size) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_get_selected_addresses(wrapper_data->agent, local, local_size, remote,
	                                     remote_size);
}

JUICE_EXPORT juice_state_t juice_wrapper_get_state(JUICE_AGENT_HANDLE handle) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	return juice_get_state(wrapper_data->agent);
}

JUICE_EXPORT void juice_wrapper_destroy(JUICE_AGENT_HANDLE handle) {
	assert((int)handle >= 0 && (int)handle < juice_wrapper_data_count);
	assert(g_juice_wrapper_datas[handle] != NULL);

	lp_juice_wrapper_data wrapper_data = g_juice_wrapper_datas[handle];
	juice_destroy(wrapper_data->agent);

	//wrapper_data->agent = NULL;
	//free(wrapper_data);

	g_juice_wrapper_datas[handle] = NULL;
}

JUICE_EXPORT void juice_wrapper_set_log_level(juice_log_level_t level) {
	JLOG_DEBUG("set log level: %d\n", level);
	juice_set_log_level(level);
}

// event handler 정의
void on_state_changed(juice_agent_t* agent, juice_state_t state, void* user_ptr) {
	JLOG_DEBUG("State: %s\n", juice_state_to_string(state));

	 //if (state == JUICE_STATE_CONNECTED) {
	 //	// Agent 1: on connected, send a message
	 //	const char* message = "Hello from 1";
	 //	juice_send(agent, message, strlen(message));
	 //}

	lp_juice_wrapper_data wrapper_data = juice_wrapper_find_data(agent);
	if (wrapper_data != NULL) {
		wrapper_data->callback_on_state_changed(wrapper_data->handle, state, user_ptr);
	} else {
		JLOG_DEBUG("Wrapper_data is NULL"); //, state, user_ptr);
	}
}

void on_candidate(juice_agent_t* agent, const char* sdp, void* user_ptr) {
	JLOG_DEBUG("Candidate: %s\n", sdp);

	//// agent의 handle을 얻어낸다
	//auto handle = juice_wrapper_find_handle(agent);
	//if (handle == 0)
	//	juice_wrapper_add_remote_candidate(handle + 1, sdp);
	//else
	//	juice_wrapper_add_remote_candidate(handle - 1, sdp);

	lp_juice_wrapper_data wrapper_data = juice_wrapper_find_data(agent);
	if (wrapper_data != NULL)
		wrapper_data->callback_on_candidate(wrapper_data->handle, sdp, user_ptr);
	else
		JLOG_DEBUG("Wrapper_data is NULL"); //, sdp, user_ptr);
}

void on_gathering_done(juice_agent_t* agent, void* user_ptr) {
	JLOG_DEBUG("Gathering done\n");

	//// agent의 handle을 얻어낸다
	//auto handle = juice_wrapper_find_handle(agent);
	//if (handle == 0)
	//	juice_wrapper_set_remote_gathering_done(handle + 1);
	//else
	//	juice_wrapper_set_remote_gathering_done(handle - 1);

	lp_juice_wrapper_data wrapper_data = juice_wrapper_find_data(agent);
	if (wrapper_data != NULL)
		wrapper_data->callback_on_gathering_done(wrapper_data->handle, user_ptr);
	else
		JLOG_DEBUG("Wrapper_data is NULL"); //, user_ptr);
}

//#define BUFFER_SIZE 100

void on_recv(juice_agent_t *agent, const char *data, size_t size, void *user_ptr) {
	 //char buffer[BUFFER_SIZE];
	 //if (size > BUFFER_SIZE - 1)
	 //	size = BUFFER_SIZE - 1;
	 //memcpy(buffer, data, size);
	 //buffer[size] = '\0';

	 JLOG_DEBUG("Received: %s\n", data);

	lp_juice_wrapper_data wrapper_data = juice_wrapper_find_data(agent);
	 if (wrapper_data != NULL)
		wrapper_data->callback_on_recv(wrapper_data->handle, data, size, user_ptr);
	 else
		JLOG_DEBUG("Wrapper_data is NULL"); //, data, size, user_ptr);
}
