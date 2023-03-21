/*
* Libjuice를 Unity plugin으로 쓰기 위해 브릿지용으로 개발되었음
*/

#ifndef PLUGIN_WRAPPER_H
#define PLUGIN_WRAPPER_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#include "juice.h"

#ifdef __cplusplus
extern "C" {
#endif

// 객체를 직접 C#에 안넘기고 handle을 넘기기 위해 버퍼를 설정한다.

// juice agent handle임
typedef unsigned int JUICE_AGENT_HANDLE;

// callback용 function pointer 정의
typedef void (*juice_wrapper_on_state_changed)(JUICE_AGENT_HANDLE handle, juice_state_t state,
                                               void *user_ptr);
typedef void (*juice_wrapper_on_candidate)(JUICE_AGENT_HANDLE handle, const char *sdp,
                                           void *user_ptr);
typedef void (*juice_wrapper_on_gathering_done)(JUICE_AGENT_HANDLE handle, void *user_ptr);
typedef void (*juice_wrapper_on_recv)(JUICE_AGENT_HANDLE handle, const char *data, size_t size,
                                      void *user_ptr);

// 초기화한다
JUICE_EXPORT void juice_wrapper_init(int agent_count);
// 할당한다
JUICE_EXPORT JUICE_AGENT_HANDLE juice_wrapper_assign_agent();

// 할당 후에 각종 정보를 지정한다.
// 
// 스턴 서버를 지정한다. host는 사본을 저장하지 않는다.
JUICE_EXPORT void juice_wrapper_set_stun(JUICE_AGENT_HANDLE handle, const char *host, int port);
// 턴 서버를 지정한다 host, username, password는 사본을 저장하지 않는다.
JUICE_EXPORT void juice_wrapper_set_turn(JUICE_AGENT_HANDLE handle, int turn_server_index,
                                         const char *host, int port, const char *username,
                                         const char *password);
// local port range를 지정합니다.
JUICE_EXPORT void juice_wrapper_local_port_range(JUICE_AGENT_HANDLE handle, int begin_port, int end_port);

// 필요한 callback을 지정한다. 지정하지 않으면 동작하지 않는다.
JUICE_EXPORT void juice_wrapper_set_callbacks(JUICE_AGENT_HANDLE handle,
                                              juice_wrapper_on_state_changed fn_on_state_changed,
                                              juice_wrapper_on_candidate fn_on_candidate,
                                              juice_wrapper_on_gathering_done fn_on_gathering_done,
                                              juice_wrapper_on_recv fn_on_recv);

// 할당해서 정보를 지정한 핸들을 생성한다.
JUICE_EXPORT int juice_wrapper_create_agent(JUICE_AGENT_HANDLE handle);

// local sdp를 얻는다
JUICE_EXPORT int juice_wrapper_get_local_description(JUICE_AGENT_HANDLE handle, char *buffer,
                                                     size_t size);
// remote sdp로 지정한다
JUICE_EXPORT int juice_wrapper_set_remote_description(JUICE_AGENT_HANDLE handle,
                                                      const char *buffer);

// ice candidates를 모은다
JUICE_EXPORT int juice_wrapper_gather_candidates(JUICE_AGENT_HANDLE handle);
// remote candidate를 설정한다
JUICE_EXPORT int juice_wrapper_add_remote_candidate(JUICE_AGENT_HANDLE handle, const char *sdp);
// remote gathering이 끝났다
JUICE_EXPORT int juice_wrapper_set_remote_gathering_done(JUICE_AGENT_HANDLE handle);
    // 선택한 ice candidate를 얻는다
JUICE_EXPORT int juice_wrapper_get_selected_candidates(JUICE_AGENT_HANDLE handle, char *local,
                                                       size_t local_size, char *remote,
                                                       size_t remote_size);
// 선택된 ice 주소를 얻는다
JUICE_EXPORT int juice_wrapper_get_selected_addresses(JUICE_AGENT_HANDLE handle, char *local,
                                              size_t local_size, char *remote, size_t remote_size);

// 상태를 얻는다
JUICE_EXPORT juice_state_t juice_wrapper_get_state(JUICE_AGENT_HANDLE handle);
// 핸들을 닫는다
JUICE_EXPORT void juice_wrapper_destroy(JUICE_AGENT_HANDLE handle);

// 로드 수준을 컨트롤한다
JUICE_EXPORT void juice_wrapper_set_log_level(juice_log_level_t level);

#ifdef __cplusplus
}
#endif

#endif
