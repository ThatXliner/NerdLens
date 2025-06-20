use base64::{engine::general_purpose, Engine as _};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tokio::io::AsyncWriteExt;
use tokio::net::TcpStream;
use tokio::sync::Mutex;

#[derive(Debug, Serialize, Deserialize)]
struct FrameData {
    data: String, // base64 encoded frame data
    width: u32,
    height: u32,
}

struct AppState {
    tcp_stream: Arc<Mutex<Option<TcpStream>>>,
}

#[tauri::command]
async fn connect_to_server(state: tauri::State<'_, AppState>) -> Result<String, String> {
    let stream = TcpStream::connect("127.0.0.1:12345")
        .await
        .map_err(|e| format!("Failed to connect to server: {}", e))?;

    *state.tcp_stream.lock().await = Some(stream);
    Ok("Connected to server".to_string())
}

#[tauri::command]
async fn disconnect_from_server(state: tauri::State<'_, AppState>) -> Result<String, String> {
    let mut stream_guard = state.tcp_stream.lock().await;
    if let Some(mut stream) = stream_guard.take() {
        let _ = stream.shutdown().await;
    }
    Ok("Disconnected from server".to_string())
}

macro_rules! reconnect {
    ($stream:ident) => {
        let _ = $stream.shutdown().await;
        println!("Disconnected");
        *$stream = TcpStream::connect("127.0.0.1:12345")
            .await
            .map_err(|e| format!("Failed to connect to server: {}", e))?;
        println!("Reconnected");
    };
}
macro_rules! write_to {
    ($stream:ident, $type: ident, $data:expr, $error:expr) => {
        if let Err(error) = $stream.write_$type($data).await {
            if attempt == 4 {
                return Err(format!($error, error));
            }
            reconnect!($stream);
            continue;
        }
    };
}

#[tauri::command]
async fn send_frame(
    frame_data: String,
    state: tauri::State<'_, AppState>,
) -> Result<String, String> {
    let mut stream_guard = state.tcp_stream.lock().await;

    if let Some(stream) = stream_guard.as_mut() {
        // Decode base64 frame data
        let frame_bytes = general_purpose::STANDARD
            .decode(&frame_data)
            .map_err(|e| format!("Failed to decode frame data: {}", e))?;

        // Send frame size as big-endian 32-bit integer
        let frame_size = frame_bytes.len() as u32;
        let size_bytes = frame_size.to_be_bytes();

        for attempt in 0..5 {
            if let Err(error) = stream.write_u8(1).await {
                if attempt == 4 {
                    return Err(format!("Failed to send message type: {}", error));
                }
                reconnect!(stream);
                continue;
            }
            if let Err(error) = stream.write_all(&size_bytes).await {
                if attempt == 4 {
                    return Err(format!("Failed to send frame size: {}", error));
                }
                reconnect!(stream);
                continue;
            }

            // Send frame data
            if let Err(error) = stream.write_all(&frame_bytes).await {
                if attempt == 4 {
                    return Err(format!("Failed to send frame data: {}", error));
                }
                reconnect!(stream);
                continue;
            }
            break;
        }
        stream
            .flush()
            .await
            .map_err(|e| format!("Failed to flush stream: {}", e))?;

        Ok("Frame sent successfully".to_string())
    } else {
        Err("Not connected to server".to_string())
    }
}
#[tauri::command]
async fn change_settings(
    settings: String,
    state: tauri::State<'_, AppState>,
) -> Result<String, String> {
    let mut stream_guard = state.tcp_stream.lock().await;

    if let Some(stream) = stream_guard.as_mut() {
        // Decode base64 frame data
        let frame_bytes = settings.as_bytes();
        // Send frame size as big-endian 32-bit integer
        let frame_size = settings.chars().count() as u32;
        let size_bytes = frame_size.to_be_bytes();

        for attempt in 0..5 {
            if let Err(error) = stream.write_u8(2).await {
                if attempt == 4 {
                    return Err(format!("Failed to send message type: {}", error));
                }
                reconnect!(stream);
                continue;
            }
            if let Err(error) = stream.write_all(&size_bytes).await {
                if attempt == 4 {
                    return Err(format!("Failed to send frame size: {}", error));
                }
                reconnect!(stream);
                continue;
            }

            // Send frame data
            if let Err(error) = stream.write_all(&frame_bytes).await {
                if attempt == 4 {
                    return Err(format!("Failed to send frame data: {}", error));
                }
                reconnect!(stream);
                continue;
            }
            break;
        }
        stream
            .flush()
            .await
            .map_err(|e| format!("Failed to flush stream: {}", e))?;

        Ok("Frame sent successfully".to_string())
    } else {
        Err("Not connected to server".to_string())
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .manage(AppState {
            tcp_stream: Arc::new(Mutex::new(None)),
        })
        .invoke_handler(tauri::generate_handler![
            connect_to_server,
            disconnect_from_server,
            send_frame,
            change_settings
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
