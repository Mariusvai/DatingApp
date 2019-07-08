export interface Message {
    id: number;
    senderId: number;
    senderKnowAs: string;
    senderPhotoUrl: string;
    recipientId: number;
    recipientKnowAs: string;
    recipientPhotoUrl: string;
    content: string;
    isRead: boolean;
    dateRead: Date;
    messageSent: Date;
}
