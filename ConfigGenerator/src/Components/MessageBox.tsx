import {Button, Label} from "@fluentui/react-components";
import {ReactElement} from "react";

function MessageBox(props: { message: string, buttonText: string | null, icon: ReactElement, action: () => any }) {
    return <div>
        <div style={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: 120,
            gap: 8,
            marginTop: 8,
            marginBottom: 8
        }}>
            {props.icon}
            <Label size="large">{props.message}</Label>
        </div>

        {props.buttonText && <div style={{display: 'flex', width: 'inherit', justifyContent: 'end', alignItems: 'end'}}>
            <Button onClick={() => props.action()}>{props.buttonText}</Button>
        </div>}

    </div>
}

export default MessageBox;