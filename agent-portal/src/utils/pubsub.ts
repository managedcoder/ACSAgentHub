import { getConversations } from '../services/conversations';
import { ApplicationContextType } from '../components/ApplicationContext';

const { WebPubSubServiceClient, Event } = require('@azure/web-pubsub');

export async function subscribeToRefreshEvents(connectionString: string, hub: string, msgHandler: ((this: WebSocket, ev: MessageEvent) => any) | null) {
    let serviceClient = new WebPubSubServiceClient(connectionString, hub);
    let token = await serviceClient.getClientAccessToken();
    let webSocket = new WebSocket(token.url);

    try {
        webSocket.onopen = function (openEvent) {
            console.log("WebSocket OPEN: " + JSON.stringify(openEvent, null, 4));

        };

        webSocket.onclose = function (closeEvent) {
            console.log("WebSocket CLOSE: " + JSON.stringify(closeEvent, null, 4));

        };

        webSocket.onerror = function (errorEvent) {
            console.log("WebSocket ERROR: " + JSON.stringify(errorEvent, null, 4));
        };

        webSocket.onmessage = msgHandler;
    } catch (exception) {
        console.error(`Could not initialize WebSocket to the Web PubSub: Exception: ${exception}`);
    }
}
